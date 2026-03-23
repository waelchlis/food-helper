using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreAdminStore(FirestoreDb db) : IAdminStore
{
    private readonly CollectionReference _admins = db.Collection("admins");
    private HashSet<string>? _cache;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> IsAdminAsync(string googleSubjectId, CancellationToken cancellationToken)
    {
        var admins = await GetCachedAdminIdsAsync(cancellationToken).ConfigureAwait(false);
        return admins.Contains(googleSubjectId);
    }

    public async Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _admins.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.Documents.Select(Map).ToList();
    }

    public async Task<AdminUser> AddByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var admin = new AdminUser { Email = email.Trim().ToLowerInvariant() };
        var docRef = await _admins.AddAsync(admin, cancellationToken).ConfigureAwait(false);
        admin.Id = docRef.Id;
        InvalidateCache();
        return admin;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var doc = _admins.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        InvalidateCache();
        return true;
    }

    public async Task<bool> TryLinkSubjectAsync(string googleSubjectId, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var snapshot = await _admins
            .WhereEqualTo("email", normalizedEmail)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        var unlinked = snapshot.Documents
            .Select(Map)
            .FirstOrDefault(a => string.IsNullOrWhiteSpace(a.GoogleSubjectId));

        if (unlinked is null)
        {
            return false;
        }

        var doc = _admins.Document(unlinked.Id);
        await doc.UpdateAsync(new Dictionary<string, object> { ["googleSubjectId"] = googleSubjectId }, cancellationToken: cancellationToken).ConfigureAwait(false);
        InvalidateCache();
        return true;
    }

    private void InvalidateCache()
    {
        _cache = null;
        _cacheExpiry = DateTime.MinValue;
    }

    private async Task<HashSet<string>> GetCachedAdminIdsAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cache;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cache;
            }

            var snapshot = await _admins.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            _cache = snapshot.Documents
                .Select(d => d.ConvertTo<AdminUser>())
                .Select(a => a.GoogleSubjectId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet();
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static AdminUser Map(DocumentSnapshot snapshot)
    {
        var admin = snapshot.ConvertTo<AdminUser>();
        admin.Id = snapshot.Id;
        return admin;
    }
}

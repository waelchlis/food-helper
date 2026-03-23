using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryAdminStore : IAdminStore
{
    private readonly List<AdminUser> _admins = [];
    private int _nextId;

    public Task<bool> IsAdminAsync(string googleSubjectId, CancellationToken cancellationToken)
    {
        if (_admins.Count == 0)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(_admins.Any(a =>
            a.GoogleSubjectId.Equals(googleSubjectId, StringComparison.Ordinal)));
    }

    public Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<AdminUser>>(_admins.ToList());
    }

    public Task<AdminUser> AddByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var admin = new AdminUser
        {
            Id = Interlocked.Increment(ref _nextId).ToString(),
            Email = email.Trim().ToLowerInvariant()
        };
        _admins.Add(admin);
        return Task.FromResult(admin);
    }

    public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        var removed = _admins.RemoveAll(a => a.Id == id) > 0;
        return Task.FromResult(removed);
    }

    public Task<bool> TryLinkSubjectAsync(string googleSubjectId, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var unlinked = _admins.FirstOrDefault(a =>
            a.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(a.GoogleSubjectId));

        if (unlinked is null)
        {
            return Task.FromResult(false);
        }

        unlinked.GoogleSubjectId = googleSubjectId;
        return Task.FromResult(true);
    }
}

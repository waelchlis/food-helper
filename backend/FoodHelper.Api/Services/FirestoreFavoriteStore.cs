using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreFavoriteStore(FirestoreDb db) : IFavoriteStore
{
    private readonly CollectionReference _favorites = db.Collection("favorites");

    public async Task<IReadOnlyList<Favorite>> GetForOwnerAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var snapshot = await _favorites
            .WhereEqualTo("ownerKey", ownerKey)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(d => d.ConvertTo<Favorite>())
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
    }

    public async Task<Favorite> AddAsync(string ownerKey, string recipeId, CancellationToken cancellationToken)
    {
        var id = DocId(ownerKey, recipeId);
        var doc = _favorites.Document(id);
        var existing = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (existing.Exists)
        {
            return existing.ConvertTo<Favorite>();
        }

        var favorite = new Favorite
        {
            Id = id,
            OwnerKey = ownerKey,
            RecipeId = recipeId,
            CreatedAt = DateTime.UtcNow,
        };

        await doc.SetAsync(favorite, cancellationToken: cancellationToken).ConfigureAwait(false);
        return favorite;
    }

    public async Task<bool> RemoveAsync(string ownerKey, string recipeId, CancellationToken cancellationToken)
    {
        var doc = _favorites.Document(DocId(ownerKey, recipeId));
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string DocId(string ownerKey, string recipeId) => $"{ownerKey}__{recipeId}";
}

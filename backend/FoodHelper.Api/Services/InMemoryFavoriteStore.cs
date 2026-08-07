using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryFavoriteStore : IFavoriteStore
{
    private readonly ConcurrentDictionary<string, Favorite> _favorites = new();

    public Task<IReadOnlyList<Favorite>> GetForOwnerAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var items = _favorites.Values
            .Where(f => f.OwnerKey == ownerKey)
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Favorite>>(items);
    }

    public Task<Favorite> AddAsync(string ownerKey, string recipeId, CancellationToken cancellationToken)
    {
        var key = DocId(ownerKey, recipeId);
        var favorite = _favorites.GetOrAdd(key, _ => new Favorite
        {
            Id = key,
            OwnerKey = ownerKey,
            RecipeId = recipeId,
            CreatedAt = DateTime.UtcNow,
        });

        return Task.FromResult(favorite);
    }

    public Task<bool> RemoveAsync(string ownerKey, string recipeId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_favorites.TryRemove(DocId(ownerKey, recipeId), out _));
    }

    private static string DocId(string ownerKey, string recipeId) => $"{ownerKey}__{recipeId}";
}

using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IFavoriteStore
{
    Task<IReadOnlyList<Favorite>> GetForOwnerAsync(string ownerKey, CancellationToken cancellationToken);
    Task<Favorite> AddAsync(string ownerKey, string recipeId, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(string ownerKey, string recipeId, CancellationToken cancellationToken);
}

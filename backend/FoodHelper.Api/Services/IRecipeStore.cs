using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IRecipeStore
{
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken);
    Task<Recipe?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Recipe> UpsertAsync(Recipe recipe, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}

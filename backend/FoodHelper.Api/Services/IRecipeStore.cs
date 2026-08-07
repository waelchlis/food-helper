using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IRecipeStore
{
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken);
    Task<Recipe?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Recipe> UpsertAsync(Recipe recipe, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);

    /// <summary>Combined filter + sort + cursor-paged query, used by the recipe list/search UI.</summary>
    Task<RecipePage> QueryAsync(RecipeQueryParameters query, CancellationToken cancellationToken);
}

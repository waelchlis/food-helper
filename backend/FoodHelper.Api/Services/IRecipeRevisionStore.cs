using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IRecipeRevisionStore
{
    /// <summary>Newest first. At most 5 per recipe are retained (see AddAsync).</summary>
    Task<IReadOnlyList<RecipeRevision>> GetForRecipeAsync(string recipeId, CancellationToken cancellationToken);

    /// <summary>Adds a revision, then prunes the oldest entries for that recipe beyond the 5-version cap.</summary>
    Task AddAsync(RecipeRevision revision, CancellationToken cancellationToken);
}

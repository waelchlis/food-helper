using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryRecipeStore : IRecipeStore
{
    private readonly ConcurrentDictionary<string, Recipe> _recipes = new();

    public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = _recipes.Values
            .OrderByDescending(r => r.UpdatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Recipe>>(items);
    }

    public Task<Recipe?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _recipes.TryGetValue(id, out var recipe);
        return Task.FromResult(recipe);
    }

    public Task<Recipe> UpsertAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        _recipes[recipe.Id] = recipe;
        return Task.FromResult(recipe);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_recipes.TryRemove(id, out _));
    }
}

using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryRecipeRevisionStore : IRecipeRevisionStore
{
    public const int MaxRevisionsPerRecipe = 5;

    private readonly ConcurrentDictionary<string, RecipeRevision> _revisions = new();

    public Task<IReadOnlyList<RecipeRevision>> GetForRecipeAsync(string recipeId, CancellationToken cancellationToken)
    {
        var items = _revisions.Values
            .Where(r => r.RecipeId == recipeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<RecipeRevision>>(items);
    }

    public Task AddAsync(RecipeRevision revision, CancellationToken cancellationToken)
    {
        _revisions[revision.Id] = revision;

        var forRecipe = _revisions.Values
            .Where(r => r.RecipeId == revision.RecipeId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(MaxRevisionsPerRecipe)
            .ToList();

        foreach (var stale in forRecipe)
        {
            _revisions.TryRemove(stale.Id, out _);
        }

        return Task.CompletedTask;
    }
}

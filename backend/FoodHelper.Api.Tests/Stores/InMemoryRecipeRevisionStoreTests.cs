using FoodHelper.Api.Models;
using FoodHelper.Api.Services;

namespace FoodHelper.Api.Tests.Stores;

public class InMemoryRecipeRevisionStoreTests
{
    private static RecipeRevision MakeRevision(string id, string recipeId, DateTime createdAt) => new()
    {
        Id = id,
        RecipeId = recipeId,
        Snapshot = new Recipe { Id = recipeId, Name = $"Snapshot {id}" },
        EditorSubject = "editor-sub",
        EditorName = "Editor",
        CreatedAt = createdAt,
    };

    [Fact]
    public async Task GetForRecipeAsync_ScopesToRecipeId_NewestFirst()
    {
        var store = new InMemoryRecipeRevisionStore();
        var now = DateTime.UtcNow;
        await store.AddAsync(MakeRevision("rev1", "recipe-a", now.AddMinutes(-2)), CancellationToken.None);
        await store.AddAsync(MakeRevision("rev2", "recipe-a", now.AddMinutes(-1)), CancellationToken.None);
        await store.AddAsync(MakeRevision("rev3", "recipe-b", now), CancellationToken.None);

        var revisionsForA = await store.GetForRecipeAsync("recipe-a", CancellationToken.None);

        Assert.Equal(["rev2", "rev1"], revisionsForA.Select(r => r.Id));
    }

    [Fact]
    public async Task AddAsync_PrunesOldestRevisions_BeyondCapOfFive()
    {
        var store = new InMemoryRecipeRevisionStore();
        var now = DateTime.UtcNow;

        for (var i = 0; i < 7; i++)
        {
            await store.AddAsync(MakeRevision($"rev{i}", "recipe-a", now.AddMinutes(i)), CancellationToken.None);
        }

        var revisions = await store.GetForRecipeAsync("recipe-a", CancellationToken.None);

        Assert.Equal(InMemoryRecipeRevisionStore.MaxRevisionsPerRecipe, revisions.Count);
        // The two oldest (rev0, rev1) should have been pruned; the five newest remain.
        Assert.Equal(["rev6", "rev5", "rev4", "rev3", "rev2"], revisions.Select(r => r.Id));
    }

    [Fact]
    public async Task AddAsync_DoesNotPruneRevisionsOfOtherRecipes()
    {
        var store = new InMemoryRecipeRevisionStore();
        var now = DateTime.UtcNow;

        for (var i = 0; i < 6; i++)
        {
            await store.AddAsync(MakeRevision($"a{i}", "recipe-a", now.AddMinutes(i)), CancellationToken.None);
        }
        await store.AddAsync(MakeRevision("b0", "recipe-b", now), CancellationToken.None);

        var revisionsForB = await store.GetForRecipeAsync("recipe-b", CancellationToken.None);

        Assert.Single(revisionsForB);
    }
}

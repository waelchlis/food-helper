using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreRecipeStore(FirestoreDb db) : IRecipeStore
{
    private readonly CollectionReference _recipes = db.Collection("recipes");

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _recipes
            .OrderByDescending("updatedAt")
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(Map)
            .ToList();
    }

    public async Task<Recipe?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var snapshot = await _recipes
            .Document(id)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!snapshot.Exists)
        {
            return null;
        }

        return Map(snapshot);
    }

    public async Task<Recipe> UpsertAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var doc = _recipes.Document(recipe.Id);
        await doc.SetAsync(recipe, cancellationToken: cancellationToken).ConfigureAwait(false);
        return recipe;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var doc = _recipes.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<RecipePage> QueryAsync(RecipeQueryParameters query, CancellationToken cancellationToken)
    {
        // Only plain equality filters are pushed to Firestore — neither needs a composite index.
        // Free-text search, ingredient matching, max-total-time, sorting, and cursor slicing all
        // run through the same RecipeQueryEngine InMemoryRecipeStore uses, over the resulting
        // candidate set, so both stores behave identically. See planning/epic-f1-recipe-discovery.md.
        Query firestoreQuery = _recipes;
        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            firestoreQuery = firestoreQuery.WhereEqualTo("categoryId", query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.DietType))
        {
            firestoreQuery = firestoreQuery.WhereEqualTo("dietType", query.DietType);
        }

        var snapshot = await firestoreQuery.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var candidates = snapshot.Documents.Select(Map);

        return RecipeQueryEngine.Apply(candidates, query);
    }

    private static Recipe Map(DocumentSnapshot snapshot)
    {
        var recipe = snapshot.ConvertTo<Recipe>();
        if (string.IsNullOrWhiteSpace(recipe.Id))
        {
            recipe.Id = snapshot.Id;
        }

        // Lazily migrate legacy single-image recipes the first time they're read, instead of
        // running a one-off migration script against production data.
        if (recipe.Images.Count == 0 && !string.IsNullOrWhiteSpace(recipe.LegacyImage))
        {
            recipe.Images = [recipe.LegacyImage];
        }

        return recipe;
    }
}

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

    private static Recipe Map(DocumentSnapshot snapshot)
    {
        var recipe = snapshot.ConvertTo<Recipe>();
        if (string.IsNullOrWhiteSpace(recipe.Id))
        {
            recipe.Id = snapshot.Id;
        }

        return recipe;
    }
}

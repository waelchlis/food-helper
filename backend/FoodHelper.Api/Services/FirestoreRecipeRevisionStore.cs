using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreRecipeRevisionStore(FirestoreDb db) : IRecipeRevisionStore
{
    public const int MaxRevisionsPerRecipe = 5;

    private readonly CollectionReference _revisions = db.Collection("recipeRevisions");

    public async Task<IReadOnlyList<RecipeRevision>> GetForRecipeAsync(string recipeId, CancellationToken cancellationToken)
    {
        // Equality + orderBy on different fields would require a Firestore composite index;
        // sort in memory instead, same as FirestoreFavoriteStore. Revisions per recipe are
        // capped at MaxRevisionsPerRecipe, so this is always a small set.
        var snapshot = await _revisions
            .WhereEqualTo("recipeId", recipeId)
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(d => d.ConvertTo<RecipeRevision>())
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public async Task AddAsync(RecipeRevision revision, CancellationToken cancellationToken)
    {
        var doc = _revisions.Document(revision.Id);
        await doc.SetAsync(revision, cancellationToken: cancellationToken).ConfigureAwait(false);

        var existing = await GetForRecipeAsync(revision.RecipeId, cancellationToken).ConfigureAwait(false);
        var stale = existing.Skip(MaxRevisionsPerRecipe).ToList();
        foreach (var revisionToPrune in stale)
        {
            await _revisions.Document(revisionToPrune.Id).DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}

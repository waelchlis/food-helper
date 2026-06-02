using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreMealPlanStore(FirestoreDb db) : IMealPlanStore
{
    private readonly CollectionReference _plans = db.Collection("mealPlans");

    public async Task<IReadOnlyList<MealPlan>> GetAllForUserAsync(string ownerKey, string userEmail, CancellationToken cancellationToken)
    {
        var email = userEmail.ToLowerInvariant();

        // Run both queries concurrently: plans owned by user + plans where user is collaborator.
        var ownerQueryTask = _plans
            .WhereEqualTo("ownerKey", ownerKey)
            .GetSnapshotAsync(cancellationToken);

        var collaboratorQueryTask = _plans
            .WhereArrayContains("collaboratorEmails", email)
            .GetSnapshotAsync(cancellationToken);

        await Task.WhenAll(ownerQueryTask, collaboratorQueryTask).ConfigureAwait(false);

        var ownerPlans = ownerQueryTask.Result.Documents.Select(Map);
        var collaboratorPlans = collaboratorQueryTask.Result.Documents.Select(Map);

        // Merge and deduplicate by ID.
        var merged = ownerPlans
            .Concat(collaboratorPlans)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderByDescending(p => p.UpdatedAt)
            .ToList();

        return merged;
    }

    public async Task<MealPlan?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var snapshot = await _plans.Document(id).GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.Exists ? Map(snapshot) : null;
    }

    public async Task<MealPlan> CreateAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        await _plans.Document(plan.Id).SetAsync(plan, cancellationToken: cancellationToken).ConfigureAwait(false);
        return plan;
    }

    public async Task<MealPlan> UpdateAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        await _plans.Document(plan.Id).SetAsync(plan, cancellationToken: cancellationToken).ConfigureAwait(false);
        return plan;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var doc = _plans.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        // Delete all entries in the subcollection first.
        var entries = await doc.Collection("entries").GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (entries.Count > 0)
        {
            var batch = _plans.Database.StartBatch();
            foreach (var entry in entries.Documents)
            {
                batch.Delete(entry.Reference);
            }
            await batch.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<MealEntry>> GetEntriesAsync(string planId, CancellationToken cancellationToken)
    {
        // Single-field ordering avoids a composite index requirement.
        // Secondary sort by createdAt is applied in memory.
        var snapshot = await _plans.Document(planId)
            .Collection("entries")
            .OrderBy("date")
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(MapEntry)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.CreatedAt)
            .ToList();
    }

    public async Task<MealEntry> AddEntryAsync(string planId, MealEntry entry, CancellationToken cancellationToken)
    {
        await _plans.Document(planId)
            .Collection("entries")
            .Document(entry.Id)
            .SetAsync(entry, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return entry;
    }

    public async Task<bool> DeleteEntryAsync(string planId, string entryId, CancellationToken cancellationToken)
    {
        var doc = _plans.Document(planId).Collection("entries").Document(entryId);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static MealPlan Map(DocumentSnapshot snapshot)
    {
        var plan = snapshot.ConvertTo<MealPlan>();
        if (string.IsNullOrWhiteSpace(plan.Id))
        {
            plan.Id = snapshot.Id;
        }
        return plan;
    }

    private static MealEntry MapEntry(DocumentSnapshot snapshot)
    {
        var entry = snapshot.ConvertTo<MealEntry>();
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            entry.Id = snapshot.Id;
        }
        return entry;
    }
}

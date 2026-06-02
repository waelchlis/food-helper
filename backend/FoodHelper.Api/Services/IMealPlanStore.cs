using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IMealPlanStore
{
    /// <summary>Returns all meal plans where the user is the owner or a collaborator.</summary>
    Task<IReadOnlyList<MealPlan>> GetAllForUserAsync(string ownerKey, string userEmail, CancellationToken cancellationToken);

    Task<MealPlan?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<MealPlan> CreateAsync(MealPlan plan, CancellationToken cancellationToken);

    Task<MealPlan> UpdateAsync(MealPlan plan, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MealEntry>> GetEntriesAsync(string planId, CancellationToken cancellationToken);

    Task<MealEntry> AddEntryAsync(string planId, MealEntry entry, CancellationToken cancellationToken);

    Task<bool> DeleteEntryAsync(string planId, string entryId, CancellationToken cancellationToken);
}

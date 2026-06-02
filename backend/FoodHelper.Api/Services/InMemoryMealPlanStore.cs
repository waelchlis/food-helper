using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryMealPlanStore : IMealPlanStore
{
    private readonly ConcurrentDictionary<string, MealPlan> _plans = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MealEntry>> _entries = new();

    public Task<IReadOnlyList<MealPlan>> GetAllForUserAsync(string ownerKey, string userEmail, CancellationToken cancellationToken)
    {
        var email = userEmail.ToLowerInvariant();
        var result = _plans.Values
            .Where(p => p.OwnerKey == ownerKey || p.CollaboratorEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UpdatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MealPlan>>(result);
    }

    public Task<MealPlan?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _plans.TryGetValue(id, out var plan);
        return Task.FromResult(plan);
    }

    public Task<MealPlan> CreateAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        _plans[plan.Id] = plan;
        _entries[plan.Id] = new ConcurrentDictionary<string, MealEntry>();
        return Task.FromResult(plan);
    }

    public Task<MealPlan> UpdateAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        _plans[plan.Id] = plan;
        return Task.FromResult(plan);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var removed = _plans.TryRemove(id, out _);
        _entries.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<IReadOnlyList<MealEntry>> GetEntriesAsync(string planId, CancellationToken cancellationToken)
    {
        if (!_entries.TryGetValue(planId, out var entries))
        {
            return Task.FromResult<IReadOnlyList<MealEntry>>([]);
        }

        var result = entries.Values.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<MealEntry>>(result);
    }

    public Task<MealEntry> AddEntryAsync(string planId, MealEntry entry, CancellationToken cancellationToken)
    {
        var entries = _entries.GetOrAdd(planId, _ => new ConcurrentDictionary<string, MealEntry>());
        entries[entry.Id] = entry;
        return Task.FromResult(entry);
    }

    public Task<bool> DeleteEntryAsync(string planId, string entryId, CancellationToken cancellationToken)
    {
        if (!_entries.TryGetValue(planId, out var entries))
        {
            return Task.FromResult(false);
        }

        var removed = entries.TryRemove(entryId, out _);
        return Task.FromResult(removed);
    }
}

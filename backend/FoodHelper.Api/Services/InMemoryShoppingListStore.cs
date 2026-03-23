using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryShoppingListStore : IShoppingListStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ShoppingListItem>> _items = new();

    public Task<IReadOnlyList<ShoppingListItem>> GetItemsAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var ownerItems = _items.GetOrAdd(ownerKey, _ => new ConcurrentDictionary<string, ShoppingListItem>());
        var result = ownerItems.Values
            .OrderBy(item => item.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<ShoppingListItem>>(result);
    }

    public Task<ShoppingListItem> UpsertItemAsync(string ownerKey, ShoppingListItem item, CancellationToken cancellationToken)
    {
        var ownerItems = _items.GetOrAdd(ownerKey, _ => new ConcurrentDictionary<string, ShoppingListItem>());
        ownerItems[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<bool> DeleteItemAsync(string ownerKey, string itemId, CancellationToken cancellationToken)
    {
        if (!_items.TryGetValue(ownerKey, out var ownerItems))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(ownerItems.TryRemove(itemId, out _));
    }

    public Task ClearAsync(string ownerKey, CancellationToken cancellationToken)
    {
        _items.TryRemove(ownerKey, out _);
        return Task.CompletedTask;
    }
}

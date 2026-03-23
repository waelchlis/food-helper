using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IShoppingListStore
{
    Task<IReadOnlyList<ShoppingListItem>> GetItemsAsync(string ownerKey, CancellationToken cancellationToken);
    Task<ShoppingListItem> UpsertItemAsync(string ownerKey, ShoppingListItem item, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(string ownerKey, string itemId, CancellationToken cancellationToken);
    Task ClearAsync(string ownerKey, CancellationToken cancellationToken);
}

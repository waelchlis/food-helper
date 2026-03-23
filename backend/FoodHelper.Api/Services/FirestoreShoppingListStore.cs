using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreShoppingListStore(FirestoreDb db) : IShoppingListStore
{
    private readonly CollectionReference _lists = db.Collection("shoppingLists");

    public async Task<IReadOnlyList<ShoppingListItem>> GetItemsAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var snapshot = await GetItemsCollection(ownerKey)
            .OrderBy("name")
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(Map)
            .ToList();
    }

    public async Task<ShoppingListItem> UpsertItemAsync(string ownerKey, ShoppingListItem item, CancellationToken cancellationToken)
    {
        var doc = GetItemsCollection(ownerKey).Document(item.Id);
        await doc.SetAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
        return item;
    }

    public async Task<bool> DeleteItemAsync(string ownerKey, string itemId, CancellationToken cancellationToken)
    {
        var doc = GetItemsCollection(ownerKey).Document(itemId);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task ClearAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var items = await GetItemsCollection(ownerKey).GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (items.Count == 0)
        {
            return;
        }

        var batch = _lists.Database.StartBatch();
        foreach (var doc in items.Documents)
        {
            batch.Delete(doc.Reference);
        }

        await batch.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private CollectionReference GetItemsCollection(string ownerKey)
    {
        return _lists.Document(ownerKey).Collection("items");
    }

    private static ShoppingListItem Map(DocumentSnapshot snapshot)
    {
        var item = snapshot.ConvertTo<ShoppingListItem>();
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = snapshot.Id;
        }

        return item;
    }
}

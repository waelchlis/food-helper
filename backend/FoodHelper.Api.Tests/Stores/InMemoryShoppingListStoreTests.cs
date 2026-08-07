using FoodHelper.Api.Models;
using FoodHelper.Api.Services;

namespace FoodHelper.Api.Tests.Stores;

public class InMemoryShoppingListStoreTests
{
    [Fact]
    public async Task GetItemsAsync_ScopesToOwnerKey()
    {
        var store = new InMemoryShoppingListStore();
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "1", Name = "Milk", Unit = "l", Amount = 1 }, CancellationToken.None);
        await store.UpsertItemAsync("session:b", new ShoppingListItem { Id = "2", Name = "Bread", Unit = "pcs", Amount = 1 }, CancellationToken.None);

        var itemsForA = await store.GetItemsAsync("session:a", CancellationToken.None);

        Assert.Single(itemsForA);
        Assert.Equal("Milk", itemsForA[0].Name);
    }

    [Fact]
    public async Task GetItemsAsync_OrdersByName()
    {
        var store = new InMemoryShoppingListStore();
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "1", Name = "Zucchini", Unit = "pcs", Amount = 1 }, CancellationToken.None);
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "2", Name = "Apples", Unit = "pcs", Amount = 1 }, CancellationToken.None);

        var items = await store.GetItemsAsync("session:a", CancellationToken.None);

        Assert.Equal(["Apples", "Zucchini"], items.Select(i => i.Name));
    }

    [Fact]
    public async Task UpsertItemAsync_PersistsCheckedFlag()
    {
        var store = new InMemoryShoppingListStore();
        var item = new ShoppingListItem { Id = "1", Name = "Eggs", Unit = "pcs", Amount = 12, Checked = true };

        await store.UpsertItemAsync("session:a", item, CancellationToken.None);
        var items = await store.GetItemsAsync("session:a", CancellationToken.None);

        Assert.True(items.Single().Checked);
    }

    [Fact]
    public async Task DeleteItemAsync_RemovesOnlyFromOwnersList()
    {
        var store = new InMemoryShoppingListStore();
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "1", Name = "Milk", Unit = "l", Amount = 1 }, CancellationToken.None);

        var deletedFromWrongOwner = await store.DeleteItemAsync("session:b", "1", CancellationToken.None);
        var deletedFromRightOwner = await store.DeleteItemAsync("session:a", "1", CancellationToken.None);

        Assert.False(deletedFromWrongOwner);
        Assert.True(deletedFromRightOwner);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllItemsForOwner()
    {
        var store = new InMemoryShoppingListStore();
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "1", Name = "Milk", Unit = "l", Amount = 1 }, CancellationToken.None);
        await store.UpsertItemAsync("session:a", new ShoppingListItem { Id = "2", Name = "Bread", Unit = "pcs", Amount = 1 }, CancellationToken.None);

        await store.ClearAsync("session:a", CancellationToken.None);
        var items = await store.GetItemsAsync("session:a", CancellationToken.None);

        Assert.Empty(items);
    }
}

using FoodHelper.Api.Services;

namespace FoodHelper.Api.Tests.Stores;

public class InMemoryFavoriteStoreTests
{
    [Fact]
    public async Task AddAsync_IsIdempotent()
    {
        var store = new InMemoryFavoriteStore();

        await store.AddAsync("user:a", "recipe-1", CancellationToken.None);
        await store.AddAsync("user:a", "recipe-1", CancellationToken.None);

        var favorites = await store.GetForOwnerAsync("user:a", CancellationToken.None);
        Assert.Single(favorites);
    }

    [Fact]
    public async Task GetForOwnerAsync_ScopesToOwner()
    {
        var store = new InMemoryFavoriteStore();
        await store.AddAsync("user:a", "recipe-1", CancellationToken.None);
        await store.AddAsync("user:b", "recipe-2", CancellationToken.None);

        var favoritesForA = await store.GetForOwnerAsync("user:a", CancellationToken.None);

        Assert.Single(favoritesForA);
        Assert.Equal("recipe-1", favoritesForA[0].RecipeId);
    }

    [Fact]
    public async Task RemoveAsync_ReturnsFalse_WhenNotFavorited()
    {
        var store = new InMemoryFavoriteStore();

        var removed = await store.RemoveAsync("user:a", "recipe-1", CancellationToken.None);

        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyMatchingFavorite()
    {
        var store = new InMemoryFavoriteStore();
        await store.AddAsync("user:a", "recipe-1", CancellationToken.None);
        await store.AddAsync("user:a", "recipe-2", CancellationToken.None);

        var removed = await store.RemoveAsync("user:a", "recipe-1", CancellationToken.None);
        var remaining = await store.GetForOwnerAsync("user:a", CancellationToken.None);

        Assert.True(removed);
        Assert.Single(remaining);
        Assert.Equal("recipe-2", remaining[0].RecipeId);
    }
}

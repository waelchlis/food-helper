using System.Net;
using System.Net.Http.Json;
using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Tests.Controllers;

public class ShoppingListControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ShoppingListControllerTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetItems_WithoutSessionIdOrAuth_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/shopping-list/items");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WithValidSessionId_Succeeds_AndIsScopedToThatSession()
    {
        _client.DefaultRequestHeaders.Add("X-Session-Id", "session-aaaaaaaaaaaa");
        var request = new UpsertShoppingListItemRequest { Name = "Milk", Amount = 1, Unit = "l" };

        var addResponse = await _client.PostAsJsonAsync("/api/shopping-list/items", request);
        addResponse.EnsureSuccessStatusCode();

        var items = await _client.GetFromJsonAsync<List<ShoppingListItem>>("/api/shopping-list/items");
        Assert.Single(items!);
        Assert.Equal("Milk", items![0].Name);

        // A different session shouldn't see it.
        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add("X-Session-Id", "session-bbbbbbbbbbbb");
        var otherItems = await otherClient.GetFromJsonAsync<List<ShoppingListItem>>("/api/shopping-list/items");
        Assert.Empty(otherItems!);
    }

    [Fact]
    public async Task UpdateItem_CanToggleChecked()
    {
        _client.DefaultRequestHeaders.Add("X-Session-Id", "session-cccccccccccc");
        var added = await (await _client.PostAsJsonAsync("/api/shopping-list/items", new UpsertShoppingListItemRequest { Name = "Eggs", Amount = 12, Unit = "pcs" }))
            .Content.ReadFromJsonAsync<ShoppingListItem>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/shopping-list/items/{added!.Id}",
            new UpsertShoppingListItemRequest { Name = "Eggs", Amount = 12, Unit = "pcs", Checked = true });

        var updated = await updateResponse.Content.ReadFromJsonAsync<ShoppingListItem>();
        Assert.True(updated!.Checked);
    }

    public void Dispose() => _factory.Dispose();
}

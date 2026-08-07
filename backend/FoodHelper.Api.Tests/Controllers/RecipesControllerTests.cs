using System.Net;
using System.Net.Http.Json;
using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Tests.Controllers;

public class RecipesControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetAll_IsAccessibleAnonymously()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/recipes");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/recipes", NewRecipeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAuthenticatedNonAdmin_ReturnsForbidden()
    {
        // A different admin already exists so this subject isn't covered by bootstrap mode.
        await _factory.MakeAdminAsync("some-other-admin", "admin@example.com");
        var client = _factory.CreateAuthenticatedClient("regular-user-sub", email: "user@example.com");

        var response = await client.PostAsJsonAsync("/api/recipes", NewRecipeRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAdmin_Succeeds_AndIsThenReadableAnonymously()
    {
        await _factory.MakeAdminAsync("admin-sub", "admin@example.com");
        var adminClient = _factory.CreateAuthenticatedClient("admin-sub", email: "admin@example.com", name: "Admin User");

        var createResponse = await adminClient.PostAsJsonAsync("/api/recipes", NewRecipeRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<Recipe>();
        Assert.NotNull(created);
        Assert.Equal("Admin User", created!.CreatorName);

        var anonymousClient = _factory.CreateClient();
        var getResponse = await anonymousClient.GetAsync($"/api/recipes/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Query_CombinesFilters()
    {
        await _factory.MakeAdminAsync("admin-sub", "admin@example.com");
        var adminClient = _factory.CreateAuthenticatedClient("admin-sub", email: "admin@example.com");
        await adminClient.PostAsJsonAsync("/api/recipes", NewRecipeRequest("Vegan Curry", dietType: "vegan"));
        await adminClient.PostAsJsonAsync("/api/recipes", NewRecipeRequest("Meat Curry", dietType: null));

        var anonymousClient = _factory.CreateClient();
        var page = await anonymousClient.GetFromJsonAsync<RecipePage>("/api/recipes?q=curry&dietType=vegan");

        Assert.NotNull(page);
        Assert.Single(page!.Items);
        Assert.Equal("Vegan Curry", page.Items[0].Name);
    }

    private static UpsertRecipeRequest NewRecipeRequest(string name = "Test Recipe", string? dietType = null) => new()
    {
        Name = name,
        Description = "A recipe used in tests.",
        Servings = 4,
        PrepTime = 10,
        CookTime = 20,
        Ingredients = [new RecipeIngredient { Id = "i1", Name = "Flour", Amount = 200, Unit = "g" }],
        Instructions = ["Mix", "Bake"],
        DietType = dietType,
    };

    public void Dispose() => _factory.Dispose();
}

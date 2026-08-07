using System.Net;
using System.Net.Http.Json;
using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Tests.Controllers;

public class MealPlansControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/meal-plans");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetById_AsOwner_Succeeds()
    {
        var owner = _factory.CreateAuthenticatedClient("owner-sub", email: "owner@example.com");

        var created = await (await owner.PostAsJsonAsync("/api/meal-plans", new UpsertMealPlanRequest { Name = "Week 1", Description = "" }))
            .Content.ReadFromJsonAsync<MealPlan>();

        var getResponse = await owner.GetAsync($"/api/meal-plans/{created!.Id}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetById_AsUnrelatedUser_ReturnsForbidden()
    {
        var owner = _factory.CreateAuthenticatedClient("owner-sub", email: "owner@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/meal-plans", new UpsertMealPlanRequest { Name = "Week 1", Description = "" }))
            .Content.ReadFromJsonAsync<MealPlan>();

        var stranger = _factory.CreateAuthenticatedClient("stranger-sub", email: "stranger@example.com");
        var response = await stranger.GetAsync($"/api/meal-plans/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AsCollaborator_Succeeds()
    {
        var owner = _factory.CreateAuthenticatedClient("owner-sub", email: "owner@example.com");
        var created = await (await owner.PostAsJsonAsync("/api/meal-plans", new UpsertMealPlanRequest { Name = "Week 1", Description = "" }))
            .Content.ReadFromJsonAsync<MealPlan>();

        await owner.PostAsJsonAsync($"/api/meal-plans/{created!.Id}/collaborators", new { email = "friend@example.com" });

        var friend = _factory.CreateAuthenticatedClient("friend-sub", email: "friend@example.com");
        var response = await friend.GetAsync($"/api/meal-plans/{created.Id}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AddEntry_ForRecipeType_DefaultsServingsToRecipesOwnServings()
    {
        await _factory.MakeAdminAsync("admin-sub", "admin@example.com");
        var admin = _factory.CreateAuthenticatedClient("admin-sub", email: "admin@example.com");
        var recipe = await (await admin.PostAsJsonAsync("/api/recipes", new UpsertRecipeRequest
        {
            Name = "Soup",
            Servings = 6,
            Ingredients = [new RecipeIngredient { Id = "i1", Name = "Water", Amount = 1, Unit = "l" }],
            Instructions = ["Boil"],
        })).Content.ReadFromJsonAsync<Recipe>();

        var owner = _factory.CreateAuthenticatedClient("owner-sub", email: "owner@example.com");
        var plan = await (await owner.PostAsJsonAsync("/api/meal-plans", new UpsertMealPlanRequest { Name = "Week 1", Description = "" }))
            .Content.ReadFromJsonAsync<MealPlan>();

        var entryResponse = await owner.PostAsJsonAsync($"/api/meal-plans/{plan!.Id}/entries", new AddMealEntryRequest
        {
            Date = "2026-01-05",
            Type = "recipe",
            RecipeId = recipe!.Id,
            RecipeName = recipe.Name,
        });

        var entry = await entryResponse.Content.ReadFromJsonAsync<MealEntry>();
        Assert.Equal(6, entry!.Servings);
    }

    public void Dispose() => _factory.Dispose();
}

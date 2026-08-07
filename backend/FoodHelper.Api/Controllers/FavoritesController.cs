using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public sealed class FavoritesController(IFavoriteStore favoriteStore, IRecipeStore recipeStore) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Recipe>>> GetAll(CancellationToken cancellationToken)
    {
        if (!User.TryGetSubject(out var subject))
        {
            return Unauthorized();
        }

        var ownerKey = $"user:{subject}";
        var favorites = await favoriteStore.GetForOwnerAsync(ownerKey, cancellationToken);
        var favoriteRecipeIds = favorites.Select(f => f.RecipeId).ToHashSet();

        var all = await recipeStore.GetAllAsync(cancellationToken);
        var favoriteRecipes = all.Where(r => favoriteRecipeIds.Contains(r.Id)).ToList();

        return Ok(favoriteRecipes);
    }

    [HttpPost("{recipeId}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Add(string recipeId, CancellationToken cancellationToken)
    {
        if (!User.TryGetSubject(out var subject))
        {
            return Unauthorized();
        }

        var recipe = await recipeStore.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        await favoriteStore.AddAsync($"user:{subject}", recipeId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{recipeId}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Remove(string recipeId, CancellationToken cancellationToken)
    {
        if (!User.TryGetSubject(out var subject))
        {
            return Unauthorized();
        }

        await favoriteStore.RemoveAsync($"user:{subject}", recipeId, cancellationToken);
        return NoContent();
    }
}

using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RecipesController(IRecipeStore recipes, IImageStore imageStore) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<Recipe>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await recipes.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Recipe>> GetById(string id, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        return Ok(recipe);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Recipe>> Create([FromBody] UpsertRecipeRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var recipe = new Recipe
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Servings = request.Servings,
            PrepTime = request.PrepTime,
            CookTime = request.CookTime,
            Ingredients = request.Ingredients,
            Instructions = request.Instructions,
            Tips = request.Tips,
            Image = request.Image,
            CategoryId = request.CategoryId,
            CreatorName = ResolveCreatorName(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await recipes.UpsertAsync(recipe, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Recipe>> Update(string id, [FromBody] UpsertRecipeRequest request, CancellationToken cancellationToken)
    {
        var existing = await recipes.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = new Recipe
        {
            Id = existing.Id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Servings = request.Servings,
            PrepTime = request.PrepTime,
            CookTime = request.CookTime,
            Ingredients = request.Ingredients,
            Instructions = request.Instructions,
            Tips = request.Tips,
            Image = request.Image,
            CategoryId = request.CategoryId,
            CreatorName = existing.CreatorName,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        return Ok(await recipes.UpsertAsync(updated, cancellationToken));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await recipes.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id}/image")]
    [Authorize(Policy = "AdminOnly")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<Recipe>> UploadImage(string id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || !file.ContentType.StartsWith("image/"))
        {
            return BadRequest(new { error = "A valid image file is required." });
        }

        var recipe = await recipes.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        using var stream = file.OpenReadStream();
        recipe.Image = await imageStore.UploadAsync(stream, file.ContentType, id, cancellationToken);
        recipe.UpdatedAt = DateTime.UtcNow;

        return Ok(await recipes.UpsertAsync(recipe, cancellationToken));
    }

    private string ResolveCreatorName()
    {
        var name = User.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var givenName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var familyName = User.FindFirstValue(ClaimTypes.Surname) ?? "";
        var fullName = $"{givenName} {familyName}".Trim();
        return fullName.Length > 0 ? fullName : "Unknown";
    }
}

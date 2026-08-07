using System.ComponentModel.DataAnnotations;
using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RecipesController(IRecipeStore recipes, IImageStore imageStore, IRecipeRevisionStore revisions) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<RecipePage>> GetAll([FromQuery] RecipeQueryParameters query, CancellationToken cancellationToken)
    {
        return Ok(await recipes.QueryAsync(query, cancellationToken));
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

    [HttpGet("{id}/similar")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<Recipe>>> GetSimilar(string id, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        var all = await recipes.GetAllAsync(cancellationToken);
        var ingredientNames = recipe.Ingredients.Select(i => i.Name.Trim().ToLowerInvariant()).ToHashSet();

        var scored = all
            .Where(r => r.Id != recipe.Id)
            .Select(r =>
            {
                var score = 0;
                if (!string.IsNullOrWhiteSpace(recipe.CategoryId) && r.CategoryId == recipe.CategoryId)
                {
                    score += 2;
                }

                score += r.Ingredients.Count(i => ingredientNames.Contains(i.Name.Trim().ToLowerInvariant()));
                return (Recipe: r, Score: score);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Recipe.UpdatedAt)
            .Take(6)
            .Select(x => x.Recipe)
            .ToList();

        return Ok(scored);
    }

    [HttpGet("export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IReadOnlyList<Recipe>>> Export(CancellationToken cancellationToken)
    {
        return Ok(await recipes.GetAllAsync(cancellationToken));
    }

    [HttpPost("import")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<IReadOnlyList<RecipeImportResult>>> Import([FromBody] List<UpsertRecipeRequest> items, CancellationToken cancellationToken)
    {
        var existing = await recipes.GetAllAsync(cancellationToken);
        var existingNames = existing.Select(r => r.Name.Trim().ToLowerInvariant()).ToHashSet();
        var results = new List<RecipeImportResult>();

        foreach (var item in items)
        {
            var validationErrors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(item, new ValidationContext(item), validationErrors, validateAllProperties: true))
            {
                results.Add(new RecipeImportResult
                {
                    Name = item.Name,
                    Success = false,
                    Error = string.Join("; ", validationErrors.Select(v => v.ErrorMessage)),
                });
                continue;
            }

            var normalizedName = item.Name.Trim().ToLowerInvariant();
            if (existingNames.Contains(normalizedName))
            {
                results.Add(new RecipeImportResult
                {
                    Name = item.Name,
                    Success = false,
                    Error = "A recipe with this name already exists.",
                });
                continue;
            }

            var now = DateTime.UtcNow;
            var recipe = new Recipe
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = item.Name.Trim(),
                Description = item.Description.Trim(),
                Servings = item.Servings,
                PrepTime = item.PrepTime,
                CookTime = item.CookTime,
                Ingredients = item.Ingredients,
                Instructions = item.Instructions,
                Tips = item.Tips,
                Images = item.Images,
                CategoryId = item.CategoryId,
                DietType = item.DietType,
                Note = item.Note,
                CreatorName = ResolveCreatorName(),
                CreatedAt = now,
                UpdatedAt = now,
            };

            var saved = await recipes.UpsertAsync(recipe, cancellationToken);
            existingNames.Add(normalizedName);
            results.Add(new RecipeImportResult { Name = saved.Name, Success = true, Id = saved.Id });
        }

        return Ok(results);
    }

    [HttpGet("{id}/history")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IReadOnlyList<RecipeRevision>>> GetHistory(string id, CancellationToken cancellationToken)
    {
        return Ok(await revisions.GetForRecipeAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
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
            Images = request.Images,
            CategoryId = request.CategoryId,
            DietType = request.DietType,
            Note = request.Note,
            CreatorName = ResolveCreatorName(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await recipes.UpsertAsync(recipe, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Recipe>> Update(string id, [FromBody] UpsertRecipeRequest request, CancellationToken cancellationToken)
    {
        var existing = await recipes.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await revisions.AddAsync(new RecipeRevision
        {
            Id = Guid.NewGuid().ToString("n"),
            RecipeId = existing.Id,
            Snapshot = existing,
            EditorSubject = User.TryGetSubject(out var subject) ? subject : string.Empty,
            EditorName = ResolveCreatorName(),
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

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
            Images = request.Images,
            CategoryId = request.CategoryId,
            DietType = request.DietType,
            Note = request.Note,
            CreatorName = existing.CreatorName,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        return Ok(await recipes.UpsertAsync(updated, cancellationToken));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var existing = await recipes.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        foreach (var image in existing.Images)
        {
            await imageStore.DeleteAsync(image, cancellationToken);
        }

        await recipes.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/image")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
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
        var uploaded = await imageStore.UploadAsync(stream, file.ContentType, id, cancellationToken);
        recipe.Images.Add(uploaded);
        recipe.UpdatedAt = DateTime.UtcNow;

        return Ok(await recipes.UpsertAsync(recipe, cancellationToken));
    }

    [HttpDelete("{id}/images")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Recipe>> RemoveImage(string id, [FromBody] RemoveRecipeImageRequest request, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        if (!recipe.Images.Remove(request.Url))
        {
            return NotFound(new { error = "Image not found on this recipe." });
        }

        await imageStore.DeleteAsync(request.Url, cancellationToken);
        recipe.UpdatedAt = DateTime.UtcNow;

        return Ok(await recipes.UpsertAsync(recipe, cancellationToken));
    }

    [HttpPut("{id}/images/order")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Recipe>> ReorderImages(string id, [FromBody] ReorderRecipeImagesRequest request, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return NotFound();
        }

        var isSamePermutation = recipe.Images.Count == request.Images.Count &&
            recipe.Images.OrderBy(i => i).SequenceEqual(request.Images.OrderBy(i => i));
        if (!isSamePermutation)
        {
            return BadRequest(new { error = "Images must be a reordering of the recipe's existing images." });
        }

        recipe.Images = request.Images;
        recipe.UpdatedAt = DateTime.UtcNow;

        return Ok(await recipes.UpsertAsync(recipe, cancellationToken));
    }

    private string ResolveCreatorName() => User.GetDisplayName();
}

public sealed class RemoveRecipeImageRequest
{
    public string Url { get; set; } = string.Empty;
}

public sealed class ReorderRecipeImagesRequest
{
    public List<string> Images { get; set; } = [];
}

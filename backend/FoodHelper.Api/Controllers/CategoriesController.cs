using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryStore categoryStore, IRecipeStore recipeStore) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await categoryStore.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Category>> Add([FromBody] CategoryNameRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var all = await categoryStore.GetAllAsync(cancellationToken);
        if (all.Any(c => c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { error = "Category already exists." });

        var category = await categoryStore.AddAsync(request.Name, cancellationToken);
        return Created($"api/categories/{category.Id}", category);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Category>> Rename(string id, [FromBody] CategoryNameRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var all = await categoryStore.GetAllAsync(cancellationToken);
        if (all.Any(c => c.Id != id && c.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { error = "A category with this name already exists." });

        var updated = await categoryStore.RenameAsync(id, request.Name, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var all = await categoryStore.GetAllAsync(cancellationToken);
        if (!all.Any(c => c.Id == id))
            return NotFound();

        var recipes = await recipeStore.GetAllAsync(cancellationToken);
        if (recipes.Any(r => r.CategoryId == id))
            return Conflict(new { error = "Cannot delete a category that is used by one or more recipes." });

        await categoryStore.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed class CategoryNameRequest
{
    public string Name { get; init; } = string.Empty;
}

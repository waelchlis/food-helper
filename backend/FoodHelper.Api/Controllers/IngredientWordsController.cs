using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/ingredient-words")]
public sealed class IngredientWordsController(IIngredientWordStore wordStore, IRecipeStore recipeStore) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<IngredientWord>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await wordStore.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IngredientWord>> Add([FromBody] AddIngredientWordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        var all = await wordStore.GetAllAsync(cancellationToken);
        if (all.Any(w => w.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { error = "Ingredient word already exists." });
        }

        var word = await wordStore.AddAsync(request.Name, cancellationToken);
        return Created($"api/ingredient-words/{word.Id}", word);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var word = (await wordStore.GetAllAsync(cancellationToken)).FirstOrDefault(w => w.Id == id);
        if (word is null)
        {
            return NotFound();
        }

        var recipes = await recipeStore.GetAllAsync(cancellationToken);
        var isUsed = recipes.Any(r => r.Ingredients.Any(i =>
            i.Name.Equals(word.Name, StringComparison.OrdinalIgnoreCase)));

        if (isUsed)
        {
            return Conflict(new { error = "Cannot delete an ingredient word that is used in a recipe." });
        }

        await wordStore.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed class AddIngredientWordRequest
{
    public string Name { get; init; } = string.Empty;
}

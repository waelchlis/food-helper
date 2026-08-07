using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/meal-plans")]
[Authorize]
public sealed class MealPlansController(
    IMealPlanStore mealPlanStore,
    IRecipeStore recipeStore,
    IShoppingListStore shoppingListStore) : ControllerBase
{
    // ── Plan CRUD ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MealPlan>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plans = await mealPlanStore.GetAllForUserAsync(ownerKey, userEmail, cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MealPlan>> GetById(string id, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!CanAccess(plan, ownerKey, userEmail))
        {
            return Forbid();
        }

        return Ok(plan);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<MealPlan>> Create([FromBody] UpsertMealPlanRequest request, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var now = DateTime.UtcNow;
        var plan = new MealPlan
        {
            Id = Guid.NewGuid().ToString("n"),
            OwnerKey = ownerKey,
            OwnerEmail = userEmail,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CollaboratorEmails = [],
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await mealPlanStore.CreateAsync(plan, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    [HttpPut("{id}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<MealPlan>> Update(string id, [FromBody] UpsertMealPlanRequest request, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out _, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!IsOwner(plan, ownerKey))
        {
            return Forbid();
        }

        plan.Name = request.Name.Trim();
        plan.Description = request.Description.Trim();
        plan.UpdatedAt = DateTime.UtcNow;

        return Ok(await mealPlanStore.UpdateAsync(plan, cancellationToken));
    }

    [HttpDelete("{id}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out _, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!IsOwner(plan, ownerKey))
        {
            return Forbid();
        }

        await mealPlanStore.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Entries ──────────────────────────────────────────────────────────────

    [HttpGet("{id}/entries")]
    public async Task<ActionResult<IReadOnlyList<MealEntry>>> GetEntries(string id, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!CanAccess(plan, ownerKey, userEmail))
        {
            return Forbid();
        }

        return Ok(await mealPlanStore.GetEntriesAsync(id, cancellationToken));
    }

    [HttpPost("{id}/entries")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<MealEntry>> AddEntry(string id, [FromBody] AddMealEntryRequest request, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!CanAccess(plan, ownerKey, userEmail))
        {
            return Forbid();
        }

        if (request.Type == "recipe" && string.IsNullOrWhiteSpace(request.RecipeId))
        {
            return BadRequest(new { error = "RecipeId is required when Type is 'recipe'." });
        }

        if (request.Type == "custom" && string.IsNullOrWhiteSpace(request.CustomText))
        {
            return BadRequest(new { error = "CustomText is required when Type is 'custom'." });
        }

        var servings = request.Servings;
        if (request.Type == "recipe" && servings is null && !string.IsNullOrWhiteSpace(request.RecipeId))
        {
            var recipe = await recipeStore.GetByIdAsync(request.RecipeId, cancellationToken);
            servings = recipe?.Servings;
        }

        var entry = new MealEntry
        {
            Id = Guid.NewGuid().ToString("n"),
            Date = request.Date,
            Type = request.Type,
            RecipeId = request.RecipeId,
            RecipeName = request.RecipeName?.Trim(),
            RecipeImage = request.RecipeImage,
            Servings = servings,
            CustomText = request.CustomText?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        // Update plan's updatedAt when a new entry is added.
        plan.UpdatedAt = DateTime.UtcNow;
        await mealPlanStore.UpdateAsync(plan, cancellationToken);

        return Ok(await mealPlanStore.AddEntryAsync(id, entry, cancellationToken));
    }

    [HttpDelete("{id}/entries/{entryId}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> DeleteEntry(string id, string entryId, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!CanAccess(plan, ownerKey, userEmail))
        {
            return Forbid();
        }

        var deleted = await mealPlanStore.DeleteEntryAsync(id, entryId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ── Shopping list generation ────────────────────────────────────────────

    [HttpPost("{id}/shopping-list")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<IReadOnlyList<ShoppingListItem>>> GenerateShoppingList(
        string id,
        [FromBody] GenerateShoppingListRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out var userEmail, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!CanAccess(plan, ownerKey, userEmail))
        {
            return Forbid();
        }

        var entries = await mealPlanStore.GetEntriesAsync(id, cancellationToken);
        var inRange = entries.Where(e =>
            e.Type == "recipe" &&
            !string.IsNullOrWhiteSpace(e.RecipeId) &&
            string.CompareOrdinal(e.Date, request.From) >= 0 &&
            string.CompareOrdinal(e.Date, request.To) <= 0);

        // Exact-match merge (same name + unit, case-insensitive), mirroring the frontend's
        // ShoppingListService.addItem behavior — see planning/epic-f7-meal-planner-integration.md.
        var merged = new Dictionary<(string Name, string Unit), double>();
        var displayNames = new Dictionary<(string Name, string Unit), string>();

        foreach (var entry in inRange)
        {
            var recipe = await recipeStore.GetByIdAsync(entry.RecipeId!, cancellationToken);
            if (recipe is null || recipe.Servings <= 0)
            {
                continue;
            }

            var plannedServings = entry.Servings ?? recipe.Servings;
            var scale = plannedServings / (double)recipe.Servings;

            foreach (var ingredient in recipe.Ingredients)
            {
                var key = (ingredient.Name.Trim().ToLowerInvariant(), ingredient.Unit.Trim().ToLowerInvariant());
                merged[key] = merged.GetValueOrDefault(key) + ingredient.Amount * scale;
                displayNames.TryAdd(key, ingredient.Name.Trim());
            }
        }

        var existingItems = await shoppingListStore.GetItemsAsync(ownerKey, cancellationToken);
        var added = new List<ShoppingListItem>();

        foreach (var (key, amount) in merged)
        {
            var existingItem = existingItems.FirstOrDefault(i =>
                i.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase) &&
                i.Unit.Equals(key.Unit, StringComparison.OrdinalIgnoreCase));

            var item = new ShoppingListItem
            {
                Id = existingItem?.Id ?? Guid.NewGuid().ToString("n"),
                Name = existingItem?.Name ?? displayNames[key],
                Amount = (existingItem?.Amount ?? 0) + amount,
                Unit = existingItem?.Unit ?? key.Unit,
                Notes = existingItem?.Notes ?? string.Empty,
                Checked = existingItem?.Checked ?? false,
                UpdatedAt = DateTime.UtcNow,
            };

            var saved = await shoppingListStore.UpsertItemAsync(ownerKey, item, cancellationToken);
            added.Add(saved);
        }

        return Ok(added);
    }

    // ── Collaborators ────────────────────────────────────────────────────────

    [HttpPost("{id}/collaborators")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<MealPlan>> AddCollaborator(string id, [FromBody] AddCollaboratorRequest request, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out _, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!IsOwner(plan, ownerKey))
        {
            return Forbid();
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // Prevent owner from adding themselves as collaborator.
        if (email == plan.OwnerEmail.ToLowerInvariant())
        {
            return BadRequest(new { error = "The plan owner cannot be added as a collaborator." });
        }

        if (!plan.CollaboratorEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            plan.CollaboratorEmails.Add(email);
            plan.UpdatedAt = DateTime.UtcNow;
            await mealPlanStore.UpdateAsync(plan, cancellationToken);
        }

        return Ok(plan);
    }

    [HttpDelete("{id}/collaborators/{email}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<MealPlan>> RemoveCollaborator(string id, string email, CancellationToken cancellationToken)
    {
        if (!TryResolveUser(out var ownerKey, out _, out var error))
        {
            return Unauthorized(new { error });
        }

        var plan = await mealPlanStore.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        if (!IsOwner(plan, ownerKey))
        {
            return Forbid();
        }

        var decodedEmail = Uri.UnescapeDataString(email).ToLowerInvariant();
        plan.CollaboratorEmails.RemoveAll(e => string.Equals(e, decodedEmail, StringComparison.OrdinalIgnoreCase));
        plan.UpdatedAt = DateTime.UtcNow;
        await mealPlanStore.UpdateAsync(plan, cancellationToken);

        return Ok(plan);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool TryResolveUser(out string ownerKey, out string userEmail, out string? error)
    {
        if (!User.TryGetSubject(out var subject))
        {
            ownerKey = string.Empty;
            userEmail = string.Empty;
            error = "Authenticated token is missing subject (sub) claim.";
            return false;
        }

        ownerKey = $"user:{subject}";
        userEmail = User.GetEmail();
        error = null;
        return true;
    }

    private static bool IsOwner(MealPlan plan, string ownerKey) =>
        plan.OwnerKey == ownerKey;

    private static bool CanAccess(MealPlan plan, string ownerKey, string userEmail) =>
        plan.OwnerKey == ownerKey ||
        plan.CollaboratorEmails.Contains(userEmail, StringComparer.OrdinalIgnoreCase);
}

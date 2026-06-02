using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/meal-plans")]
[Authorize]
public sealed class MealPlansController(IMealPlanStore mealPlanStore) : ControllerBase
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

        var entry = new MealEntry
        {
            Id = Guid.NewGuid().ToString("n"),
            Date = request.Date,
            Type = request.Type,
            RecipeId = request.RecipeId,
            RecipeName = request.RecipeName?.Trim(),
            RecipeImage = request.RecipeImage,
            CustomText = request.CustomText?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        // Update plan's updatedAt when a new entry is added.
        plan.UpdatedAt = DateTime.UtcNow;
        await mealPlanStore.UpdateAsync(plan, cancellationToken);

        return Ok(await mealPlanStore.AddEntryAsync(id, entry, cancellationToken));
    }

    [HttpDelete("{id}/entries/{entryId}")]
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

    // ── Collaborators ────────────────────────────────────────────────────────

    [HttpPost("{id}/collaborators")]
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
        var subject =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("nameidentifier");

        if (string.IsNullOrWhiteSpace(subject))
        {
            ownerKey = string.Empty;
            userEmail = string.Empty;
            error = "Authenticated token is missing subject (sub) claim.";
            return false;
        }

        var email =
            User.FindFirstValue("email") ??
            User.FindFirstValue(ClaimTypes.Email) ??
            string.Empty;

        ownerKey = $"user:{subject}";
        userEmail = email.ToLowerInvariant();
        error = null;
        return true;
    }

    private static bool IsOwner(MealPlan plan, string ownerKey) =>
        plan.OwnerKey == ownerKey;

    private static bool CanAccess(MealPlan plan, string ownerKey, string userEmail) =>
        plan.OwnerKey == ownerKey ||
        plan.CollaboratorEmails.Contains(userEmail, StringComparer.OrdinalIgnoreCase);
}

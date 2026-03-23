using FoodHelper.Api.Contracts;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/shopping-list")]
public sealed class ShoppingListController(IShoppingListStore shoppingListStore) : ControllerBase
{
    [HttpGet("items")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ShoppingListItem>>> GetItems(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!OwnerKeyResolver.TryResolve(User, sessionId, out var ownerKey, out var error))
        {
            return BadRequest(new { error });
        }

        var items = await shoppingListStore.GetItemsAsync(ownerKey, cancellationToken);
        return Ok(items);
    }

    [HttpPost("items")]
    [AllowAnonymous]
    public async Task<ActionResult<ShoppingListItem>> AddItem(
        [FromBody] UpsertShoppingListItemRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!OwnerKeyResolver.TryResolve(User, sessionId, out var ownerKey, out var error))
        {
            return BadRequest(new { error });
        }

        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = request.Name.Trim(),
            Amount = request.Amount,
            Unit = request.Unit.Trim(),
            Notes = request.Notes.Trim(),
            UpdatedAt = DateTime.UtcNow,
        };

        var saved = await shoppingListStore.UpsertItemAsync(ownerKey, item, cancellationToken);
        return Ok(saved);
    }

    [HttpPut("items/{itemId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShoppingListItem>> UpdateItem(
        string itemId,
        [FromBody] UpsertShoppingListItemRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!OwnerKeyResolver.TryResolve(User, sessionId, out var ownerKey, out var error))
        {
            return BadRequest(new { error });
        }

        var item = new ShoppingListItem
        {
            Id = itemId,
            Name = request.Name.Trim(),
            Amount = request.Amount,
            Unit = request.Unit.Trim(),
            Notes = request.Notes.Trim(),
            UpdatedAt = DateTime.UtcNow,
        };

        var saved = await shoppingListStore.UpsertItemAsync(ownerKey, item, cancellationToken);
        return Ok(saved);
    }

    [HttpDelete("items/{itemId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteItem(
        string itemId,
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!OwnerKeyResolver.TryResolve(User, sessionId, out var ownerKey, out var error))
        {
            return BadRequest(new { error });
        }

        var deleted = await shoppingListStore.DeleteItemAsync(ownerKey, itemId, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("items")]
    [AllowAnonymous]
    public async Task<IActionResult> ClearItems(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!OwnerKeyResolver.TryResolve(User, sessionId, out var ownerKey, out var error))
        {
            return BadRequest(new { error });
        }

        await shoppingListStore.ClearAsync(ownerKey, cancellationToken);
        return NoContent();
    }
}

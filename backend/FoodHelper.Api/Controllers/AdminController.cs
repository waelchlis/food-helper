using FoodHelper.Api.Contracts;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(IAdminStore adminStore) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var admins = await adminStore.GetAllAsync(cancellationToken);
        return Ok(admins.Select(a => new { a.Id, a.Email, isLinked = !string.IsNullOrWhiteSpace(a.GoogleSubjectId) }));
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Add([FromBody] AddAdminRequest request, CancellationToken cancellationToken)
    {
        var admin = await adminStore.AddByEmailAsync(request.Email, cancellationToken);
        return Created($"/api/admin/{admin.Id}", new { admin.Id, admin.Email, isLinked = false });
    }

    [HttpDelete("{id}")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<IActionResult> Remove(string id, CancellationToken cancellationToken)
    {
        User.TryGetSubject(out var currentSubject);

        var admins = await adminStore.GetAllAsync(cancellationToken);
        var target = admins.FirstOrDefault(a => a.Id == id);

        if (target is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(target.GoogleSubjectId)
            && target.GoogleSubjectId == currentSubject)
        {
            return BadRequest(new { error = "You cannot remove yourself as admin." });
        }

        var removed = await adminStore.RemoveAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}

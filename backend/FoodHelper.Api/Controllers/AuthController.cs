using System.Security.Claims;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHelper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAdminStore adminStore) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return BadRequest(new { error = "Missing subject claim." });
        }

        var isAdmin = await adminStore.IsAdminAsync(subject, cancellationToken);

        if (!isAdmin)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var linked = await adminStore.TryLinkSubjectAsync(subject, email, cancellationToken);
                if (linked)
                {
                    isAdmin = true;
                }
            }
        }

        return Ok(new { isAdmin });
    }
}

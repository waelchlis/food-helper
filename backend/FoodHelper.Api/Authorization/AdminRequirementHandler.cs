using System.Security.Claims;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace FoodHelper.Api.Authorization;

public sealed class AdminRequirementHandler(IAdminStore adminStore) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return;
        }

        if (await adminStore.IsAdminAsync(subject, CancellationToken.None).ConfigureAwait(false))
        {
            context.Succeed(requirement);
        }
    }
}

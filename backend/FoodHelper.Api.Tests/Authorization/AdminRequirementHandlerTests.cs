using System.Security.Claims;
using FoodHelper.Api.Authorization;
using FoodHelper.Api.Models;
using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace FoodHelper.Api.Tests.Authorization;

public class AdminRequirementHandlerTests
{
    private static AuthorizationHandlerContext ContextFor(ClaimsPrincipal user) =>
        new([new AdminRequirement()], user, resource: null);

    [Fact]
    public async Task Succeeds_WhenSubjectIsRecognizedAdmin()
    {
        var store = new InMemoryAdminStore();
        var admin = await store.AddByEmailAsync("admin@example.com", CancellationToken.None);
        await store.TryLinkSubjectAsync("admin-sub", "admin@example.com", CancellationToken.None);

        var handler = new AdminRequirementHandler(store);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "admin-sub")], "test"));
        var context = ContextFor(user);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_WhenSubjectIsNotAnAdmin()
    {
        // InMemoryAdminStore treats an empty admin list as bootstrap mode (everyone is admin),
        // so register an unrelated admin first to exercise the real "not an admin" path.
        var store = new InMemoryAdminStore();
        await store.AddByEmailAsync("someone-else@example.com", CancellationToken.None);

        var handler = new AdminRequirementHandler(store);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "random-sub")], "test"));
        var context = ContextFor(user);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Fails_WhenUserHasNoSubjectClaim()
    {
        var store = new InMemoryAdminStore();
        var handler = new AdminRequirementHandler(store);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "no-sub@example.com")], "test"));
        var context = ContextFor(user);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

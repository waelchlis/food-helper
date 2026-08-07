using System.Security.Claims;
using FoodHelper.Api.Services;

namespace FoodHelper.Api.Tests.Services;

public class OwnerKeyResolverTests
{
    private static ClaimsPrincipal AuthenticatedUser(string subject) =>
        new(new ClaimsIdentity([new Claim("sub", subject)], "test"));

    private static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());

    [Fact]
    public void TryResolve_AuthenticatedUser_ReturnsUserPrefixedKey()
    {
        var user = AuthenticatedUser("abc123");

        var result = OwnerKeyResolver.TryResolve(user, sessionId: null, out var ownerKey, out var error);

        Assert.True(result);
        Assert.Equal("user:abc123", ownerKey);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolve_AuthenticatedUserWithoutSubjectClaim_Fails()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "a@b.com")], "test"));

        var result = OwnerKeyResolver.TryResolve(user, sessionId: null, out var ownerKey, out var error);

        Assert.False(result);
        Assert.Equal(string.Empty, ownerKey);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryResolve_AnonymousUserWithValidSessionId_ReturnsSessionPrefixedKey()
    {
        var user = AnonymousUser();

        var result = OwnerKeyResolver.TryResolve(user, sessionId: "a-valid-session-id-1234", out var ownerKey, out var error);

        Assert.True(result);
        Assert.Equal("session:a-valid-session-id-1234", ownerKey);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("has a space in it 1234")]
    public void TryResolve_AnonymousUserWithInvalidSessionId_Fails(string? sessionId)
    {
        var user = AnonymousUser();

        var result = OwnerKeyResolver.TryResolve(user, sessionId, out var ownerKey, out var error);

        Assert.False(result);
        Assert.Equal(string.Empty, ownerKey);
        Assert.NotNull(error);
    }
}

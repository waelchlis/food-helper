using System.Security.Claims;
using FoodHelper.Api.Services;

namespace FoodHelper.Api.Tests.Services;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void TryGetSubject_PrefersSubClaim()
    {
        var user = PrincipalWith(new Claim("sub", "sub-123"), new Claim(ClaimTypes.NameIdentifier, "other-id"));

        var result = user.TryGetSubject(out var subject);

        Assert.True(result);
        Assert.Equal("sub-123", subject);
    }

    [Fact]
    public void TryGetSubject_FallsBackToNameIdentifierClaim()
    {
        var user = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "id-456"));

        var result = user.TryGetSubject(out var subject);

        Assert.True(result);
        Assert.Equal("id-456", subject);
    }

    [Fact]
    public void TryGetSubject_FallsBackToRawNameidentifierClaim()
    {
        var user = PrincipalWith(new Claim("nameidentifier", "id-789"));

        var result = user.TryGetSubject(out var subject);

        Assert.True(result);
        Assert.Equal("id-789", subject);
    }

    [Fact]
    public void TryGetSubject_ReturnsFalseWhenNoSubjectClaimPresent()
    {
        var user = PrincipalWith(new Claim("email", "someone@example.com"));

        var result = user.TryGetSubject(out var subject);

        Assert.False(result);
        Assert.Equal(string.Empty, subject);
    }

    [Fact]
    public void GetEmail_NormalizesToLowercase()
    {
        var user = PrincipalWith(new Claim("email", "Someone@Example.COM"));

        Assert.Equal("someone@example.com", user.GetEmail());
    }

    [Fact]
    public void GetEmail_FallsBackToClaimTypesEmail()
    {
        var user = PrincipalWith(new Claim(ClaimTypes.Email, "Fallback@Example.com"));

        Assert.Equal("fallback@example.com", user.GetEmail());
    }

    [Fact]
    public void GetEmail_ReturnsEmptyStringWhenAbsent()
    {
        var user = PrincipalWith(new Claim("sub", "abc"));

        Assert.Equal(string.Empty, user.GetEmail());
    }

    [Fact]
    public void GetDisplayName_PrefersNameClaim()
    {
        var user = PrincipalWith(new Claim("name", "Ada Lovelace"), new Claim(ClaimTypes.GivenName, "Ada"));

        Assert.Equal("Ada Lovelace", user.GetDisplayName());
    }

    [Fact]
    public void GetDisplayName_FallsBackToGivenAndFamilyName()
    {
        var user = PrincipalWith(new Claim(ClaimTypes.GivenName, "Grace"), new Claim(ClaimTypes.Surname, "Hopper"));

        Assert.Equal("Grace Hopper", user.GetDisplayName());
    }

    [Fact]
    public void GetDisplayName_FallsBackToUnknownWhenNoNameClaimsPresent()
    {
        var user = PrincipalWith(new Claim("sub", "abc"));

        Assert.Equal("Unknown", user.GetDisplayName());
    }
}

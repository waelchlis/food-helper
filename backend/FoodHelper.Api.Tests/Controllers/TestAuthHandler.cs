using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodHelper.Api.Tests.Controllers;

/// <summary>
/// Replaces real Google JWT validation in tests. A request carrying an "X-Test-Sub" header is
/// authenticated as that subject (plus optional X-Test-Email / X-Test-Name); a request without
/// it is treated as anonymous, exactly like an unauthenticated request against the real app.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subjectValues) || string.IsNullOrWhiteSpace(subjectValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new("sub", subjectValues.ToString()) };

        if (Request.Headers.TryGetValue("X-Test-Email", out var email) && !string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-Name", out var name) && !string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim("name", name.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

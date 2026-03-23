using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FoodHelper.Api.Services;

public static class OwnerKeyResolver
{
    private static readonly Regex SessionIdPattern = new("^[A-Za-z0-9._-]{8,128}$", RegexOptions.Compiled);

    public static bool TryResolve(ClaimsPrincipal user, string? sessionId, out string ownerKey, out string? error)
    {
        if (user.Identity?.IsAuthenticated == true)
        {
            var subject =
                user.FindFirstValue("sub") ??
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("nameidentifier");

            if (string.IsNullOrWhiteSpace(subject))
            {
                ownerKey = string.Empty;
                error = "Authenticated token is missing subject (sub) claim.";
                return false;
            }

            ownerKey = $"user:{subject}";
            error = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(sessionId) || !SessionIdPattern.IsMatch(sessionId))
        {
            ownerKey = string.Empty;
            error = "Provide a valid X-Session-Id header when not authenticated.";
            return false;
        }

        ownerKey = $"session:{sessionId}";
        error = null;
        return true;
    }
}

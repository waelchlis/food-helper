using System.Security.Claims;

namespace FoodHelper.Api.Services;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetSubject(this ClaimsPrincipal user, out string subject)
    {
        subject =
            user.FindFirstValue("sub") ??
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("nameidentifier") ??
            string.Empty;

        return !string.IsNullOrWhiteSpace(subject);
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        var email =
            user.FindFirstValue("email") ??
            user.FindFirstValue(ClaimTypes.Email) ??
            string.Empty;

        return email.ToLowerInvariant();
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        var name = user.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var givenName = user.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var familyName = user.FindFirstValue(ClaimTypes.Surname) ?? "";
        var fullName = $"{givenName} {familyName}".Trim();
        return fullName.Length > 0 ? fullName : "Unknown";
    }
}

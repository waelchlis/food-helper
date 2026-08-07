namespace FoodHelper.Api.Services;

public static class RateLimitPolicies
{
    public const string Writes = "writes";
}

/// <summary>
/// Resolves the same identity already used for data ownership (user:{sub} or session:{X-Session-Id})
/// so rate limits are scoped per user/session rather than per raw request. The app has a small
/// user base (well under 50 users), so this favors basic abuse protection over strict capacity
/// management — see planning/epic-nf4-auth-consolidation-rate-limiting.md.
/// </summary>
public static class RateLimitKeyResolver
{
    public static string Resolve(HttpContext context)
    {
        if (context.User.TryGetSubject(out var subject))
        {
            return $"user:{subject}";
        }

        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return $"session:{sessionId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
    }
}

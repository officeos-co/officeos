namespace EnterpriseAgentOs.Api.Middleware;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAuthMiddleware> _logger;

    private static readonly string[] SkipPrefixes =
    [
        "/api/auth/google",
        "/api/auth/callback",
        "/api/auth/logout",
        "/api/health",
        "/healthz",
        "/api/graphql",
        "/api/agents/me/",
        "/api/runner/register",
        "/api/runner/jobs",
        "/api/runner/heartbeat",
        "/api/runner/skills",
        "/api/runner/device/code",
        "/api/runner/device/token",
        "/api/billing/webhook",
    ];

    public SessionAuthMiddleware(RequestDelegate next, ILogger<SessionAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (SkipPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var cookie = context.Request.Cookies["eaos-session"];
        if (string.IsNullOrWhiteSpace(cookie))
        {
            _logger.LogDebug("No eaos-session cookie on {Path}", path);
            await _next(context);
            return;
        }

        try
        {
            var decoded = Uri.UnescapeDataString(cookie);
            var tokenHash = HashToken(decoded);

            var sessionRepo = context.RequestServices.GetRequiredService<EnterpriseAgentOs.Api.Entities.Auth.ISessionRepository>();
            var session = await sessionRepo.GetByTokenHashAsync(tokenHash);

            if (session is null)
            {
                _logger.LogWarning("Session not found for hash {HashPrefix}... on {Path}",
                    tokenHash[..8], path);
                await _next(context);
                return;
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Session expired for user {UserId} on {Path} (expired {Expiry})",
                    session.UserId, path, session.ExpiresAt);
                await _next(context);
                return;
            }

            if (session.User is null)
            {
                _logger.LogWarning("Session has no user loaded for user ID {UserId} on {Path}",
                    session.UserId, path);
                await _next(context);
                return;
            }

            _logger.LogDebug("Authenticated {Email} on {Path}", session.User.Email, path);
            context.Items["User"] = session.User;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session auth failed on {Path}", path);
        }

        await _next(context);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}

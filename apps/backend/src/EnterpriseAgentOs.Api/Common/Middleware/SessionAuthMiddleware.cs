namespace EnterpriseAgentOs.Api.Common.Middleware;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ILogger<SessionAuthMiddleware> _logger;
    private readonly string[] _skipPrefixes;

    private static readonly TimeSpan SessionCacheTtl = TimeSpan.FromMinutes(5);

    public SessionAuthMiddleware(RequestDelegate next, ILogger<SessionAuthMiddleware> logger, SessionAuthConfig config)
    {
        _requestDelegate = next;
        _logger = logger;
        _skipPrefixes = config.SkipPrefixes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (_skipPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _requestDelegate(context);
            return;
        }

        var cookie = context.Request.Cookies["eaos-session"];
        if (string.IsNullOrWhiteSpace(cookie))
        {
            _logger.LogDebug("No eaos-session cookie on {Path}", path);
            await _requestDelegate(context);
            return;
        }

        try
        {
            var decoded = Uri.UnescapeDataString(cookie);
            var tokenHash = HashToken(decoded);
            var cacheKey = $"session:{tokenHash[..16]}";

            var cache = context.RequestServices.GetRequiredService<IDistributedCache>();
            var cachedUser = await cache.GetJsonAsync<UserRecord>(cacheKey, context.RequestAborted);

            if (cachedUser is null)
            {
                var sessionRepo = context.RequestServices.GetRequiredService<ISessionRepository>();
                var session = await sessionRepo.GetByTokenHashAsync(tokenHash);

                if (session is null)
                {
                    _logger.LogWarning("Session not found for hash {HashPrefix}... on {Path}",
                        tokenHash[..8], path);
                    await _requestDelegate(context);
                    return;
                }

                if (session.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Session expired for user {UserId} on {Path} (expired {Expiry})",
                        session.UserId, path, session.ExpiresAt);
                    await _requestDelegate(context);
                    return;
                }

                if (session.User is null)
                {
                    _logger.LogWarning("Session has no user loaded for user ID {UserId} on {Path}",
                        session.UserId, path);
                    await _requestDelegate(context);
                    return;
                }

                cachedUser = session.User;
                await cache.SetJsonAsync(cacheKey, cachedUser, SessionCacheTtl, context.RequestAborted);
            }

            _logger.LogDebug("Authenticated {Email} on {Path}", cachedUser.Email, path);
            context.Items["User"] = cachedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session auth failed on {Path}", path);
        }

        await _requestDelegate(context);
    }

    public static string HashToken(string token) => SessionTokenHasher.Hash(token);
}

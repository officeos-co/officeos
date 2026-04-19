namespace EnterpriseAgentOs.Api.Middleware;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAuthMiddleware> _logger;
    private readonly string[] _skipPrefixes;

    private static readonly TimeSpan SessionCacheTtl = TimeSpan.FromMinutes(5);

    public SessionAuthMiddleware(RequestDelegate next, ILogger<SessionAuthMiddleware> logger, SessionAuthConfig config)
    {
        _next = next;
        _logger = logger;
        _skipPrefixes = config.SkipPrefixes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (_skipPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
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
            var cacheKey = $"session:{tokenHash[..16]}";

            var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

            if (!cache.TryGetValue(cacheKey, out UserRecord? cachedUser) || cachedUser is null)
            {
                var sessionRepo = context.RequestServices.GetRequiredService<ISessionRepository>();
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

                cachedUser = session.User;
                cache.Set(cacheKey, cachedUser, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = SessionCacheTtl,
                });
            }

            _logger.LogDebug("Authenticated {Email} on {Path}", cachedUser.Email, path);
            context.Items["User"] = cachedUser;
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

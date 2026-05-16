namespace OffceOs.Api.Common.Middleware;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private readonly string[] _skipPrefixes;

    private static readonly TimeSpan SessionCacheTtl = TimeSpan.FromMinutes(5);

    public SessionAuthMiddleware(RequestDelegate next, SessionAuthConfig config)
    {
        _requestDelegate = next;
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

        var token = ResolveToken(context);
        if (string.IsNullOrWhiteSpace(token))
        {
            await _requestDelegate(context);
            return;
        }

        try
        {
            var decoded = Uri.UnescapeDataString(token);
            var tokenHash = HashToken(decoded);
            var cacheKey = $"session:{tokenHash[..16]}";

            var cache = context.RequestServices.GetRequiredService<IDistributedCache>();
            var cachedUser = await cache.GetJsonAsync<UserRecord>(cacheKey, context.RequestAborted);

            if (cachedUser is null)
            {
                var sessionRepo = context.RequestServices.GetRequiredService<ISessionRepository>();
                var session = await sessionRepo.GetByAsync(new SessionFilter { TokenHash = tokenHash });

                if (session is null)
                {
                    await AppendWarningAsync(context, "Session not found for hash {HashPrefix} on {Path}", [tokenHash[..8], path]);
                    await _requestDelegate(context);
                    return;
                }

                if (session.ExpiresAt < DateTime.UtcNow)
                {
                    await AppendWarningAsync(context, "Session expired for user {UserId} on {Path}", [session.UserId, path]);
                    await _requestDelegate(context);
                    return;
                }

                if (session.User is null)
                {
                    await AppendWarningAsync(context, "Session has no user loaded for user {UserId} on {Path}", [session.UserId, path]);
                    await _requestDelegate(context);
                    return;
                }

                cachedUser = session.User;
                await cache.SetJsonAsync(cacheKey, cachedUser, SessionCacheTtl, context.RequestAborted);
            }

            context.Items["User"] = cachedUser;
        }
        catch (Exception ex)
        {
            await context.RequestServices
                .GetRequiredService<IResourceLogWriterService>()
                .ForControlPlane()
                .ErrorAsync(ex, "Session auth failed on {Path}", path, context.RequestAborted);
        }

        await _requestDelegate(context);
    }

    public static string HashToken(string token) => SessionTokenHasher.Hash(token);

    private static Task AppendWarningAsync(HttpContext context, string messageTemplate, IReadOnlyList<object?> values) =>
        context.RequestServices
            .GetRequiredService<IResourceLogWriterService>()
            .ForControlPlane()
            .WarningAsync(messageTemplate, values, context.RequestAborted);

    private static string? ResolveToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        return context.Request.Cookies["eaos-session"];
    }
}

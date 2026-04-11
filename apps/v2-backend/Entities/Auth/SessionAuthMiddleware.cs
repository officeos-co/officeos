using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAgentOs.Api.Entities.Auth;

public sealed class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] SkipPrefixes =
    [
        "/api/auth/",
        "/api/health",
        "/healthz",
        "/api/graphql",
        "/api/agents/me/",
        "/api/runner/",
    ];

    public SessionAuthMiddleware(RequestDelegate next) => _next = next;

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
            await _next(context);
            return;
        }

        var tokenHash = HashToken(cookie);
        var sessionRepo = context.RequestServices.GetRequiredService<ISessionRepository>();
        var session = await sessionRepo.GetByTokenHashAsync(tokenHash);

        if (session is null || session.ExpiresAt < DateTime.UtcNow || session.User is null)
        {
            await _next(context);
            return;
        }

        context.Items["User"] = session.User;
        await _next(context);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}

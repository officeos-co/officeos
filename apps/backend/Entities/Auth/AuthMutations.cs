namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AuthMutations
{
    /// <summary>
    /// Clears the current dashboard session. Returns true if a session
    /// was deleted, false if the caller had no active session.
    /// Requires a valid session to invoke (enforced by DashboardAuthMiddleware);
    /// anonymous callers should instead simply drop their cookie client-side.
    /// </summary>
    public async Task<bool> Logout(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Auth.ISessionRepository sessions,
        CancellationToken ct)
    {
        var http = context.Service<IHttpContextAccessor>().HttpContext;
        var cookie = http?.Request.Cookies["eaos-session"];
        if (string.IsNullOrEmpty(cookie))
            return false;

        var tokenHash = EnterpriseAgentOs.Api.Middleware.SessionAuthMiddleware.HashToken(cookie);
        await sessions.DeleteAsync(tokenHash, ct);
        http!.Response.Cookies.Delete("eaos-session");
        return true;
    }
}

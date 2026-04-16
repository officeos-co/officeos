using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
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
        [Service] ISessionRepository sessions,
        CancellationToken ct)
    {
        var http = context.Service<IHttpContextAccessor>().HttpContext;
        var cookie = http?.Request.Cookies["eaos-session"];
        if (string.IsNullOrEmpty(cookie))
            return false;

        var tokenHash = SessionAuthMiddleware.HashToken(cookie);
        await sessions.DeleteAsync(tokenHash, ct);
        http!.Response.Cookies.Delete("eaos-session");
        return true;
    }
}

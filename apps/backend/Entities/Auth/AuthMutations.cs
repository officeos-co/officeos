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
    /// <summary>
    /// Updates editable profile fields on the authenticated user.
    /// Any null field is left unchanged.
    /// </summary>
    public async Task<EnterpriseAgentOs.Api.Entities.Auth.Types.UserPayload> UpdateProfile(
        EnterpriseAgentOs.Api.Entities.Auth.Types.UpdateProfileInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Auth.IUserRepository users,
        CancellationToken ct)
    {
        var caller = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var user = await users.UpdateProfileAsync(
            caller.Id,
            input.Name,
            input.DisplayName,
            input.Timezone,
            input.NotificationPrefsJson,
            input.Preferences,
            ct);
        return new EnterpriseAgentOs.Api.Entities.Auth.Types.UserPayload(
            user.Id,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.DisplayName,
            user.Timezone,
            user.NotificationPrefsJson,
            user.Preferences);
    }

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

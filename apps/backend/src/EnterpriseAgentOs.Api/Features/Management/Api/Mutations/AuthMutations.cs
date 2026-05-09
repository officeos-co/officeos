namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
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
    [GraphQLDescription("Updates editable profile fields on the authenticated user. Null fields are left unchanged.")]
    public async Task<UserPayload> UpdateProfile(
        UpdateProfileInput input,
        [Service] UserContext user,
        [Service] IUserRepository users,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var updated = await users.UpdateProfileAsync(
            user.Id,
            input.Name,
            input.DisplayName,
            input.Timezone,
            input.NotificationPrefsJson,
            input.Preferences,
            ct);
        await cache.RemoveAsync($"auth:me:{user.Id}", ct);
        return new UserPayload(
            updated.Id,
            updated.Email,
            updated.Name,
            updated.AvatarUrl,
            updated.DisplayName,
            updated.Timezone,
            updated.NotificationPrefsJson,
            updated.Preferences);
    }

    [GraphQLDescription("Clears the current dashboard session cookie. Returns true if a session was deleted.")]
    public async Task<bool> Logout(
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ISessionRepository sessions,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var http = httpContextAccessor.HttpContext;
        var cookie = http?.Request.Cookies["eaos-session"];
        if (string.IsNullOrEmpty(cookie))
            return false;

        var tokenHash = SessionAuthMiddleware.HashToken(cookie);
        await sessions.DeleteAsync(tokenHash, ct);
        await cache.RemoveAsync($"session:{tokenHash[..16]}", ct);
        http!.Response.Cookies.Delete("eaos-session");
        return true;
    }

    /// <summary>
    /// Permanently deletes all data owned by the authenticated user.
    /// Invalidates the current session.
    /// </summary>
    [GraphQLDescription("Permanently deletes all data owned by the authenticated user (GDPR right-to-erasure). Invalidates the current session.")]
    public async Task<bool> PurgeMyData(
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] UserContext user,
        [Service] IGdprService gdpr,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        await gdpr.PurgeAsync(user.Id, ct);

        // Evict the session cache so the old cookie is immediately invalid
        var http = httpContextAccessor.HttpContext;
        var cookie = http?.Request.Cookies["eaos-session"];
        if (!string.IsNullOrEmpty(cookie))
        {
            var tokenHash = SessionAuthMiddleware.HashToken(cookie);
            await cache.RemoveAsync($"session:{tokenHash[..16]}", ct);
        }

        return true;
    }
}

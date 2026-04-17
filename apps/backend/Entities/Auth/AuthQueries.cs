namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AuthQueries
{
    /// <summary>
    /// Returns the currently authenticated dashboard user, or throws UNAUTHENTICATED.
    /// Replaces the REST <c>GET /api/auth/me</c> endpoint.
    /// </summary>
    public async Task<EnterpriseAgentOs.Api.Entities.Auth.Types.UserPayload> Me(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Auth.IUserRepository users,
        CancellationToken ct)
    {
        var ctxUser = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        // Reload from the DB so profile fields (DisplayName, Timezone, NotificationPrefsJson) are current.
        var user = await users.GetByIdAsync(ctxUser.Id, ct) ?? ctxUser;
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
}

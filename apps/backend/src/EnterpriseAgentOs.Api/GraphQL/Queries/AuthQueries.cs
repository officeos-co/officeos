namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuthQueries
{
    /// <summary>
    /// Returns the currently authenticated dashboard user, or throws UNAUTHENTICATED.
    /// Replaces the REST <c>GET /api/auth/me</c> endpoint.
    /// </summary>
    public async Task<Types.UserPayload> Me(
        IResolverContext context,
        [Service] IUserRepository users,
        CancellationToken ct)
    {
        var ctxUser = Middleware.DashboardAuthContextExtensions.GetUser(context);
        // Reload from the DB so profile fields (DisplayName, Timezone, NotificationPrefsJson) are current.
        var user = await users.GetByIdAsync(ctxUser.Id, ct) ?? ctxUser;
        return new Types.UserPayload(
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

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AuthQueries
{
    /// <summary>
    /// Returns the currently authenticated dashboard user, or throws UNAUTHENTICATED.
    /// Replaces the REST <c>GET /api/auth/me</c> endpoint.
    /// </summary>
    public EnterpriseAgentOs.Api.Entities.Auth.Types.UserPayload Me(IResolverContext context)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return new EnterpriseAgentOs.Api.Entities.Auth.Types.UserPayload(user.Id, user.Email, user.Name, user.AvatarUrl);
    }
}

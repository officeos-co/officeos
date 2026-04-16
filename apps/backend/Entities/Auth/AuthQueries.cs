using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AuthQueries
{
    /// <summary>
    /// Returns the currently authenticated dashboard user, or throws UNAUTHENTICATED.
    /// Replaces the REST <c>GET /api/auth/me</c> endpoint.
    /// </summary>
    public UserPayload Me(IResolverContext context)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        return new UserPayload(user.Id, user.Email, user.Name, user.AvatarUrl);
    }
}

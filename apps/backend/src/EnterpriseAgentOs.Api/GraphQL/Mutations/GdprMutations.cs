namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class GdprMutations
{
    /// <summary>
    /// Permanently deletes all data owned by the authenticated user.
    /// Invalidates the current session.
    /// </summary>
    public async Task<bool> PurgeMyData(
        IResolverContext context,
        [Service] IGdprService gdpr,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        await gdpr.PurgeAsync(user.Id, ct);
        return true;
    }
}

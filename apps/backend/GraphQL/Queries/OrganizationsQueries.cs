namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class OrganizationsQueries
{
    /// <summary>
    /// Returns the caller's organization. Single-tenant-compatible: each user is
    /// auto-assigned to a default org that is created on first access. The model
    /// already supports multiple orgs per instance for future multi-tenancy.
    /// </summary>
    public async Task<Types.OrganizationPayload> Org(
        IResolverContext context,
        [Service] IOrganizationRepository orgs,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        var members = await orgs.ListMembersAsync(org.Id, ct);
        return new Types.OrganizationPayload(
            org.Id, org.Name, org.OwnerUserId, org.CreatedAt,
            members.Select(m => new Types.OrgMemberPayload(
                m.Id, m.OrganizationId, m.UserId, m.Email, null, m.Role, m.Status, m.CreatedAt)).ToList());
    }
}

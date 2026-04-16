namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class OrganizationsQueries
{
    /// <summary>
    /// Returns the caller's organization. Single-tenant-compatible: each user is
    /// auto-assigned to a default org that is created on first access. The model
    /// already supports multiple orgs per instance for future multi-tenancy.
    /// </summary>
    public async Task<EnterpriseAgentOs.Api.Entities.Organizations.Types.OrganizationPayload> Org(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Organizations.IOrganizationRepository orgs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        var members = await orgs.ListMembersAsync(org.Id, ct);
        return new EnterpriseAgentOs.Api.Entities.Organizations.Types.OrganizationPayload(
            org.Id, org.Name, org.OwnerUserId, org.CreatedAt,
            members.Select(m => new EnterpriseAgentOs.Api.Entities.Organizations.Types.OrgMemberPayload(
                m.Id, m.OrganizationId, m.UserId, m.Email, null, m.Role, m.Status, m.CreatedAt)).ToList());
    }
}

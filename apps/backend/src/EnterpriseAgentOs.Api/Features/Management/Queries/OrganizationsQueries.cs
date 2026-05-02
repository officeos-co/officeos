namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class OrganizationsQueries
{
    private static readonly TimeSpan OrgCacheTtl = TimeSpan.FromMinutes(5);

    [GraphQLDescription("Returns the authenticated user's organization (auto-created on first call) with member list. Cached for 5 minutes.")]
    public async Task<OrganizationPayload> Org(
        IResolverContext context,
        [Service] IOrganizationRepository orgs,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"org:dashboard:{user.Id}";

        var cached = await cache.GetJsonAsync<OrganizationPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        var members = await orgs.ListMembersAsync(org.Id, ct);
        var result = new OrganizationPayload(
            org.Id, org.Name, org.OwnerUserId, org.CreatedAt, members);

        await cache.SetJsonAsync(cacheKey, result, OrgCacheTtl, ct);
        return result;
    }
}

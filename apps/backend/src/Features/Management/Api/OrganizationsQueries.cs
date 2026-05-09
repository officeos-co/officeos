namespace EnterpriseAgentOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class OrganizationsQueries
{
    private static readonly TimeSpan OrgCacheTtl = TimeSpan.FromMinutes(5);

    [GraphQLDescription("Returns the authenticated user's organization (auto-created on first call) with member list. Cached for 5 minutes.")]
    public async Task<OrganizationPayload> Org(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var cacheKey = $"org:dashboard:{user.Id}";

        var cached = await cache.GetJsonAsync<OrganizationPayload>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var overview = await orgs.GetOverviewAsync(user.Id, user.Email, user.Name, ct);
        var org = overview.Organization;
        var result = new OrganizationPayload(
            org.Id, org.Name, org.OwnerUserId, org.CreatedAt, overview.Members);

        await cache.SetJsonAsync(cacheKey, result, OrgCacheTtl, ct);
        return result;
    }
}

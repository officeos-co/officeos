namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class OrganizationsQueries
{
    private static readonly TimeSpan OrgCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<Types.OrganizationPayload> Org(
        IResolverContext context,
        [Service] IOrganizationRepository orgs,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"org:dashboard:{user.Id}";

        if (cache.TryGetValue(cacheKey, out Types.OrganizationPayload? cached) && cached is not null)
            return cached;

        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        var members = await orgs.ListMembersAsync(org.Id, ct);
        var result = new Types.OrganizationPayload(
            org.Id, org.Name, org.OwnerUserId, org.CreatedAt,
            members.Select(m => new Types.OrgMemberPayload(
                m.Id, m.OrganizationId, m.UserId, m.Email, null, m.Role, m.Status, m.CreatedAt)).ToList());

        cache.Set(cacheKey, result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = OrgCacheTtl });
        return result;
    }
}

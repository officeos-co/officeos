namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class AccessGroupQueries
{
    public async Task<IReadOnlyList<AccessGroupPayload>> GetAccessGroups(
        Guid organizationId,
        [Service] UserContext user,
        [Service] IAccessGroupService accessGroups,
        CancellationToken ct)
    {
        var groups = await accessGroups.ListAsync(user.Id, organizationId, ct);
        return groups.Select(AccessGroupGraphQLMapper.ToPayload).ToList();
    }
}

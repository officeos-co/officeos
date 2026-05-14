namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class OrganizationPolicyQueries
{
    public async Task<OrganizationPolicyProfilePayload> GetOrganizationPolicyProfile(
        Guid organizationId,
        [Service] UserContext user,
        [Service] IOrganizationPolicyService organizationPolicyService,
        CancellationToken ct)
    {
        var profile = await organizationPolicyService.GetOrCreateAsync(user.Id, organizationId, ct);
        return OrganizationPolicyGraphQLMapper.ToPayload(profile);
    }
}

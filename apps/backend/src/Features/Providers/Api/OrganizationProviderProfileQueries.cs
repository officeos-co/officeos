namespace OffceOs.Api.Features.Providers;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class OrganizationProviderProfileQueries
{
    public async Task<IReadOnlyList<OrganizationProviderProfilePayload>> GetOrganizationProviderProfiles(
        Guid organizationId,
        [Service] UserContext user,
        [Service] IOrganizationProviderProfileService organizationProviderProfileService,
        CancellationToken ct)
    {
        var profiles = await organizationProviderProfileService.ListAsync(user.Id, organizationId, ct);
        return profiles.Select(OrganizationProviderProfileGraphQLMapper.ToPayload).ToList();
    }
}

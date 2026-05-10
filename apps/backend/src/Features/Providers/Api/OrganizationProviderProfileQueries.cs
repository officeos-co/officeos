namespace OffceOs.Api.Features.Providers;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class OrganizationProviderProfileQueries
{
    public async Task<IReadOnlyList<ProviderSetupStatusPayload>> GetProviderSetupStatus(
        Guid organizationId,
        [Service] UserContext user,
        [Service] IProviderSetupService providerSetupService,
        CancellationToken ct)
    {
        var status = await providerSetupService.GetSetupStatusAsync(user.Id, organizationId, ct);
        return status.Select(OrganizationProviderProfileGraphQLMapper.ToPayload).ToList();
    }

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

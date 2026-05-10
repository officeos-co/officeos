namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class OrganizationProviderProfileMutations
{
    public async Task<OrganizationProviderProfilePayload> SaveOrganizationProviderProfile(
        SaveOrganizationProviderProfileInput input,
        [Service] UserContext user,
        [Service] IOrganizationProviderProfileService organizationProviderProfileService,
        CancellationToken ct)
    {
        try
        {
            var profile = await organizationProviderProfileService.SaveAsync(
                user.Id,
                input.OrganizationId,
                input.Provider,
                input.DisplayName,
                input.AllowedModels,
                input.ApiKey,
                input.Enabled,
                ct);
            return OrganizationProviderProfileGraphQLMapper.ToPayload(profile);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<bool> DeleteOrganizationProviderProfile(
        Guid organizationId,
        string provider,
        [Service] UserContext user,
        [Service] IOrganizationProviderProfileService organizationProviderProfileService,
        CancellationToken ct)
    {
        return await organizationProviderProfileService.DeleteAsync(user.Id, organizationId, provider, ct);
    }
}

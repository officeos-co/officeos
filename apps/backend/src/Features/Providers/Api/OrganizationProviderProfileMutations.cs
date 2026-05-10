namespace OffceOs.Api.Features.Providers;

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
            var profile = string.IsNullOrWhiteSpace(input.AuthKind)
                ? await organizationProviderProfileService.SaveAsync(
                    user.Id,
                    input.OrganizationId,
                    input.Provider,
                    input.DisplayName,
                    input.AllowedModels,
                    input.ApiKey,
                    input.Enabled,
                    ct)
                : await organizationProviderProfileService.SaveNativeAuthAsync(
                    user.Id,
                    input.OrganizationId,
                    input.Provider,
                    input.DisplayName,
                    input.AllowedModels,
                    input.AuthKind.ToProviderAuthKind(),
                    input.Credentials?.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase)
                        ?? new Dictionary<string, string>(),
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

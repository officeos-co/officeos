namespace OffceOs.Api.Features.Providers;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class OrganizationProviderProfileMutations
{
    public async Task<OrganizationProviderProfilePayload> SaveBedrockProviderSetup(
        BedrockProviderSetupInput input,
        [Service] UserContext user,
        [Service] IProviderSetupService providerSetupService,
        CancellationToken ct)
    {
        try
        {
            var profile = await providerSetupService.SaveBedrockSetupAsync(
                user.Id,
                new BedrockProviderSetupRequest(
                    input.OrganizationId,
                    input.DisplayName,
                    input.AwsRegion,
                    input.AuthKind.ToProviderAuthKind(),
                    input.AwsProfile,
                    input.AwsAccessKeyId,
                    input.AwsSecretAccessKey,
                    input.AwsSessionToken,
                    input.BedrockApiKey,
                    input.BaseUrl,
                    input.SkipProviderAuth,
                    input.PinnedModels,
                    input.Enabled),
                ct);
            return OrganizationProviderProfileGraphQLMapper.ToPayload(profile);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<OrganizationProviderProfilePayload> SaveVertexProviderSetup(
        VertexProviderSetupInput input,
        [Service] UserContext user,
        [Service] IProviderSetupService providerSetupService,
        CancellationToken ct)
    {
        try
        {
            var profile = await providerSetupService.SaveVertexSetupAsync(
                user.Id,
                new VertexProviderSetupRequest(
                    input.OrganizationId,
                    input.DisplayName,
                    input.ProjectId,
                    input.Location,
                    input.AuthKind.ToProviderAuthKind(),
                    input.CredentialsPath,
                    input.BaseUrl,
                    input.SkipProviderAuth,
                    input.PinnedModels,
                    input.Enabled),
                ct);
            return OrganizationProviderProfileGraphQLMapper.ToPayload(profile);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<OrganizationProviderProfilePayload> SaveFoundryProviderSetup(
        FoundryProviderSetupInput input,
        [Service] UserContext user,
        [Service] IProviderSetupService providerSetupService,
        CancellationToken ct)
    {
        try
        {
            var profile = await providerSetupService.SaveFoundrySetupAsync(
                user.Id,
                new FoundryProviderSetupRequest(
                    input.OrganizationId,
                    input.DisplayName,
                    input.Resource,
                    input.BaseUrl,
                    input.AuthKind.ToProviderAuthKind(),
                    input.ApiKey,
                    input.SkipProviderAuth,
                    input.PinnedModels,
                    input.Enabled),
                ct);
            return OrganizationProviderProfileGraphQLMapper.ToPayload(profile);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<ProviderModelAccessCheckPayload> CheckProviderModelAccess(
        ProviderModelAccessCheckInput input,
        [Service] UserContext user,
        [Service] IProviderSetupService providerSetupService,
        CancellationToken ct)
    {
        try
        {
            var result = await providerSetupService.CheckModelAccessAsync(
                user.Id,
                input.OrganizationId,
                input.Provider,
                input.Model,
                ct);
            return OrganizationProviderProfileGraphQLMapper.ToPayload(result);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

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

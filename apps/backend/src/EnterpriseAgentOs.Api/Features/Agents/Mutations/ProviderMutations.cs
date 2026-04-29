namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ProviderMutations
{
    [GraphQLDescription("Sets the API key for an LLM provider. Only available in self-hosted (Development) mode.")]
    public async Task<ProviderGqlDto> SetProviderKey(
        string providerName,
        string apiKey,
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        if (!ValueManager.IsDevelopment())
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Provider keys can only be configured in self-hosted (Development) mode.")
                    .SetCode("FORBIDDEN")
                    .Build());
        var dto = await providers.ConfigureAsync(providerName, apiKey, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Provider '{providerName}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return ProviderGraphQLMapper.ToDto(dto);
    }

    [GraphQLDescription("Removes the API key for an LLM provider. Only available in self-hosted (Development) mode.")]
    public async Task<ProviderGqlDto> ClearProviderKey(
        string providerName,
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        if (!ValueManager.IsDevelopment())
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Provider keys can only be configured in self-hosted (Development) mode.")
                    .SetCode("FORBIDDEN")
                    .Build());

        await providers.ClearAsync(providerName, ct);
        var all = await providers.ListAsync(ct);
        var row = all.FirstOrDefault(p =>
            string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Provider '{providerName}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return ProviderGraphQLMapper.ToDto(row);
    }
}

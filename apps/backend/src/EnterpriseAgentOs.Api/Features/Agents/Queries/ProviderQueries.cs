namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    [GraphQLDescription("Lists all configured LLM providers with name, display name, and whether an API key is set.")]
    public async Task<IReadOnlyList<ProviderDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await providers.ListAsync(ct);
    }

    [GraphQLDescription("Returns available model IDs for a specific provider name.")]
    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return KnownModels.For(providerName);
    }

    [GraphQLDescription("Returns all supported models across all providers with display names and default indicator.")]
    public IReadOnlyList<ModelInfoDto> GetSupportedModels(
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return KnownModels.SupportedModels
            .Select(m => new ModelInfoDto(
                m,
                KnownModels.GetDisplayName(m),
                m == KnownModels.DefaultModel))
            .ToList();
    }
}

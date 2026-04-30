namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    [GraphQLDescription("Lists all configured LLM providers with name, display name, models, and whether an API key is set.")]
    public async Task<IReadOnlyList<ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var list = await providers.ListAsync(ct);
        return list.Select(ProviderGraphQLMapper.ToDto).ToList();
    }

    [GraphQLDescription("Returns available model IDs for a specific provider name.")]
    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return ProviderRegistry.GetModelIds(providerName);
    }

    [GraphQLDescription("Returns available models with display names and default indicator. In self-hosted mode, only configured providers' models are returned.")]
    public async Task<IReadOnlyList<ModelInfoDto>> GetSupportedModels(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        // Production/Staging: platform has keys for all providers
        if (!context.Service<IWebHostEnvironment>().IsDevelopment())
        {
            return ProviderRegistry.SupportedModels
                .Select(m => new ModelInfoDto(m, ProviderRegistry.GetDisplayName(m), m == ProviderRegistry.DefaultModel))
                .ToList();
        }

        // Development (self-hosted): only models from user-configured providers
        var configured = await providers.ListAsync(ct);
        var configuredNames = configured
            .Where(p => p.Configured)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var models = configuredNames
            .SelectMany(name => ProviderRegistry.GetModelIds(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (models.Count > 0)
            models.Insert(0, "auto");

        return models
            .Select(m => new ModelInfoDto(m, ProviderRegistry.GetDisplayName(m), m == ProviderRegistry.DefaultModel))
            .ToList();
    }
}

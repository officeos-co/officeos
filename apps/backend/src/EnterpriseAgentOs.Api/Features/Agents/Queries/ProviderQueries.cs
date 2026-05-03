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

        var configured = await providers.ListAsync(ct);
        var configuredNames = configured
            .Where(p => p.Configured)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var models = configured
            .Where(p => p.Configured)
            .SelectMany(p => ProviderRegistry.GetModelIds(p.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var includeAuto = configuredNames.Contains("anthropic") && models.Count > 0;
        if (includeAuto)
            models.Insert(0, ProviderRegistry.DefaultModel);

        return models
            .Select((m, index) => new ModelInfoDto(
                m,
                ProviderRegistry.GetDisplayName(m),
                includeAuto ? m == ProviderRegistry.DefaultModel : index == 0))
            .ToList();
    }
}

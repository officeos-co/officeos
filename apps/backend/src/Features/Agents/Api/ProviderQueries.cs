namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    [GraphQLDescription("Lists all configured LLM providers with name, display name, models, and whether an API key is set.")]
    public async Task<IReadOnlyList<ProviderGqlDto>> GetProviders(
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var list = await providers.ListAsync(ct);
        return list.Select(ProviderGraphQLMapper.ToDto).ToList();
    }

    [GraphQLDescription("Returns available model IDs for a specific provider name.")]
    public async Task<IReadOnlyList<string>> GetProviderModels(
        string providerName,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var configured = await providers.ListAsync(ct);
        var provider = configured.FirstOrDefault(p =>
            string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

        return provider is not null
            ? provider.Models.Select(m => m.Id).ToList()
            : ProviderRegistry.GetModelIds(providerName);
    }

    [GraphQLDescription("Returns available models with display names and default indicator. In self-hosted mode, only configured providers' models are returned.")]
    public async Task<IReadOnlyList<ModelInfoDto>> GetSupportedModels(
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var configured = await providers.ListAsync(ct);
        var configuredNames = configured
            .Where(p => p.Configured)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var models = configured
            .Where(p => p.Configured)
            .SelectMany(p => p.Models.Select(m => (Provider: p.Name, Model: m)))
            .GroupBy(x => x.Model.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var includeAuto = configuredNames.Contains("anthropic") && models.Count > 0;
        if (includeAuto)
        {
            models.Insert(0, (
                "anthropic",
                new ProviderModelResult(
                    ProviderRegistry.DefaultModel,
                    ProviderRegistry.GetDisplayName(ProviderRegistry.DefaultModel),
                    0)));
        }

        return models
            .Select((m, index) => new ModelInfoDto(
                m.Model.Id,
                m.Model.DisplayName,
                m.Provider,
                includeAuto ? m.Model.Id == ProviderRegistry.DefaultModel : index == 0))
            .ToList();
    }
}

namespace OffceOs.Api.Features.Providers;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    [GraphQLDescription("Lists all configured LLM providers with name, display name, models, and whether an API key is set.")]
    public async Task<IReadOnlyList<ProviderPayload>> GetProviders(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var list = await providers.ListForWorkspaceAsync(workspace.Id, ct);
        return list.Select(ProviderGraphQLMapper.ToPayload).ToList();
    }

    [GraphQLDescription("Returns available model IDs for a specific provider name.")]
    public async Task<IReadOnlyList<string>> GetProviderModels(
        string providerName,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var configured = await providers.ListForWorkspaceAsync(workspace.Id, ct);
        var provider = configured.FirstOrDefault(p =>
            string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));

        if (provider is not null)
            return provider.Models.Select(m => m.Id).ToList();

        return ProviderRegistry.IsEnterpriseProvider(providerName)
            ? []
            : ProviderRegistry.GetModelIds(providerName);
    }

    [GraphQLDescription("Returns available models with display names and default indicator. In self-hosted mode, only configured providers' models are returned.")]
    public async Task<IReadOnlyList<ModelInfoPayload>> GetSupportedModels(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var configured = await providers.ListForWorkspaceAsync(workspace.Id, ct);
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
            .Select((m, index) => new ModelInfoPayload(
                m.Model.Id,
                m.Model.DisplayName,
                m.Provider,
                includeAuto ? m.Model.Id == ProviderRegistry.DefaultModel : index == 0))
            .ToList();
    }
}

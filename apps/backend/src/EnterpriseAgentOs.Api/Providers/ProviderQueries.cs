namespace EnterpriseAgentOs.Api.Providers;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await providers.ListAsync(ct);
        return rows.Select(ProviderGraphQLMapper.ToDto).ToList();
    }

    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return KnownModels.For(providerName);
    }

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

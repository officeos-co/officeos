namespace EnterpriseAgentOs.Api.Features.Providers;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<ProviderDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await providers.ListAsync(ct);
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

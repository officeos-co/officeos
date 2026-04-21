namespace EnterpriseAgentOs.Api.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await providers.ListAsync(ct);
        return rows.Select(ProviderGraphQLMapper.ToDto).ToList();
    }

    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return Domain.Services.KnownModels.For(providerName);
    }

    public IReadOnlyList<ModelInfoDto> GetSupportedModels(
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return Domain.Services.KnownModels.SupportedModels
            .Select(m => new ModelInfoDto(
                m,
                Domain.Services.KnownModels.GetDisplayName(m),
                m == Domain.Services.KnownModels.DefaultModel))
            .ToList();
    }
}

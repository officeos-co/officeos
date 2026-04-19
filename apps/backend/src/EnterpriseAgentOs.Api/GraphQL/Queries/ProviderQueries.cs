namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<Types.ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await providers.ListAsync(ct);
        return rows.Select(Types.ProviderGraphQLMapper.ToDto).ToList();
    }

    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return Domain.Services.KnownModels.For(providerName);
    }

    public IReadOnlyList<Types.ModelInfoDto> GetSupportedModels(
        IResolverContext context)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return Domain.Services.KnownModels.SupportedModels
            .Select(m => new Types.ModelInfoDto(
                m,
                Domain.Services.KnownModels.GetDisplayName(m),
                m == Domain.Services.KnownModels.DefaultModel))
            .ToList();
    }
}

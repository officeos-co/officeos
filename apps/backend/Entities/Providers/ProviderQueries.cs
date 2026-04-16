namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Providers.IProviderService providers,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await providers.ListAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGraphQLMapper.ToDto).ToList();
    }

    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return EnterpriseAgentOs.Api.Entities.Providers.KnownModels.For(providerName);
    }
}

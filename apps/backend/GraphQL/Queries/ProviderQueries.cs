namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class ProviderQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.ProviderGqlDto>> GetProviders(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Providers.IProviderService providers,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await providers.ListAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.GraphQL.Types.ProviderGraphQLMapper.ToDto).ToList();
    }

    public IReadOnlyList<string> GetProviderModels(
        string providerName,
        IResolverContext context)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return EnterpriseAgentOs.Application.Services.Providers.KnownModels.For(providerName);
    }
}

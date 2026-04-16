using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

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
}

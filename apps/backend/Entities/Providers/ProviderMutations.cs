using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ProviderMutations
{
    public async Task<ProviderGqlDto> SetProviderKey(
        string providerName,
        string apiKey,
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dto = await providers.ConfigureAsync(providerName, apiKey, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Provider '{providerName}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return ProviderGraphQLMapper.ToDto(dto);
    }

    public async Task<ProviderGqlDto> ClearProviderKey(
        string providerName,
        IResolverContext context,
        [Service] IProviderService providers,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await providers.ClearAsync(providerName, ct);
        var all = await providers.ListAsync(ct);
        var row = all.FirstOrDefault(p =>
            string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Provider '{providerName}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return ProviderGraphQLMapper.ToDto(row);
    }
}

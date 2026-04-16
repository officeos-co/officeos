namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class ProviderMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGqlDto> SetProviderKey(
        string providerName,
        string apiKey,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Providers.IProviderService providers,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await providers.ConfigureAsync(providerName, apiKey, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Provider '{providerName}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGraphQLMapper.ToDto(dto);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGqlDto> ClearProviderKey(
        string providerName,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Providers.IProviderService providers,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return EnterpriseAgentOs.Api.Entities.Providers.Types.ProviderGraphQLMapper.ToDto(row);
    }
}

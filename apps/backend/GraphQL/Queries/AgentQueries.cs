namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentQueries
{
    public Task<IReadOnlyList<EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto>> GetAgents(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return agents.ListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto?> GetAgent(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agents.GetAsync(id, ct);
    }
}

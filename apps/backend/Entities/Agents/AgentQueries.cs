namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentQueries
{
    public Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Agents.AgentDto>> GetAgents(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return agents.ListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Agents.AgentDto?> GetAgent(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agents.GetAsync(id, ct);
    }
}

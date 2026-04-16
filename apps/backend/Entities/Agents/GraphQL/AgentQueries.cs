using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.Agents.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentQueries
{
    public Task<IReadOnlyList<AgentDto>> GetAgents(
        IResolverContext context,
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return agents.ListAsync(ct);
    }

    public async Task<AgentDto?> GetAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await agents.GetAsync(id, ct);
    }
}

using HotChocolate.Execution;

namespace EnterpriseAgentOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLSubscriptions))]
public class AgentLogSubscriptions
{
    [Subscribe(With = nameof(SubscribeAgentLogAppended))]
    [GraphQLDescription("Streams newly appended log entries for one agent.")]
    public AgentLogDto AgentLogAppended([EventMessage] AgentLogDto log) => log;

    public async ValueTask<ISourceStream<AgentLogDto>> SubscribeAgentLogAppended(
        Guid agentId,
        IResolverContext context,
        [Service] ITopicEventReceiver receiver,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await receiver.SubscribeAsync<AgentLogDto>(AgentLogTopics.AgentLogAppended(agentId), ct);
    }
}

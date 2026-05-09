namespace OffceOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLSubscriptions))]
public class AgentLogSubscriptions
{
    [Subscribe(With = nameof(SubscribeAgentLogAppended))]
    [GraphQLDescription("Streams newly appended log entries for one agent.")]
    public AgentLogProjection AgentLogAppended([EventMessage] AgentLogProjection log) => log;

    public async ValueTask<HotChocolate.Execution.ISourceStream<AgentLogProjection>> SubscribeAgentLogAppended(
        Guid agentId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken ct)
    {
        return await receiver.SubscribeAsync<AgentLogProjection>(AgentLogTopics.AgentLogAppended(agentId), ct);
    }
}

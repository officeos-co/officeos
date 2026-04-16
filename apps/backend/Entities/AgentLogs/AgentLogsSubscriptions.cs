using System.Runtime.CompilerServices;

namespace EnterpriseAgentOs.Api.Subscriptions;

[ExtendObjectType(typeof(GraphQLSubscriptions))]
public class AgentLogsSubscriptions
{
    [Subscribe(With = nameof(SubscribeAgentLog))]
    public AgentLogDto AgentLogAppended(
        Guid agentId,
        [EventMessage] AgentLogDto log) => log;

    public async IAsyncEnumerable<AgentLogDto> SubscribeAgentLog(
        Guid agentId,
        [Service] ITopicEventReceiver receiver,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var src = await receiver.SubscribeAsync<AgentLogDto>($"agent-log:{agentId}", ct);
        await foreach (var msg in src.ReadEventsAsync().WithCancellation(ct))
        {
            yield return msg;
        }
    }
}

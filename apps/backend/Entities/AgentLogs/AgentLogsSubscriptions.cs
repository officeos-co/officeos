namespace EnterpriseAgentOs.Api.Subscriptions;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLSubscriptions))]
public class AgentLogsSubscriptions
{
    [Subscribe(With = nameof(SubscribeAgentLog))]
    public EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto AgentLogAppended(
        Guid agentId,
        [EventMessage] EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto log) => log;

    public async IAsyncEnumerable<EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto> SubscribeAgentLog(
        Guid agentId,
        [Service] ITopicEventReceiver receiver,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var src = await receiver.SubscribeAsync<EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto>($"agent-log:{agentId}", ct);
        await foreach (var msg in src.ReadEventsAsync().WithCancellation(ct))
        {
            yield return msg;
        }
    }
}

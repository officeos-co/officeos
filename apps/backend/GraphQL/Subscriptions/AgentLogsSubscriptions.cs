namespace EnterpriseAgentOs.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLSubscriptions))]
public class AgentLogsSubscriptions
{
    [Subscribe(With = nameof(SubscribeAgentLog))]
    public EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogDto AgentLogAppended(
        Guid agentId,
        [EventMessage] EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogDto log) => log;

    public async IAsyncEnumerable<EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogDto> SubscribeAgentLog(
        Guid agentId,
        [Service] ITopicEventReceiver receiver,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var src = await receiver.SubscribeAsync<EnterpriseAgentOs.Domain.DTOs.AgentLogs.AgentLogDto>($"agent-log:{agentId}", ct);
        await foreach (var msg in src.ReadEventsAsync().WithCancellation(ct))
        {
            yield return msg;
        }
    }
}

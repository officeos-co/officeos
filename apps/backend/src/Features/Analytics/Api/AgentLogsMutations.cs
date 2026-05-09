namespace EnterpriseAgentOs.Api.Features.Analytics;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentLogsMutations
{
    [GraphQLDescription("Sends a user message to an agent. Creates a MessageIn log entry and triggers the agent turn pipeline.")]
    public async Task<AgentLogProjection> SendAgentMessage(
        Guid agentId,
        string content,
        [Service] UserContext user,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Message content must not be empty.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        var saved = await logs.SendMessageAsync(agentId, content, user.Id, ct);
        return AgentLogMapper.ToProjection(saved);
    }

    [GraphQLDescription("Appends an arbitrary log entry to an agent's timeline. Used by the dashboard for system events.")]
    public async Task<AgentLogProjection> AppendAgentLog(
        AppendAgentLogInput input,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var record = new AgentLogRecord
        {
            AgentId = input.AgentId,
            Time = DateTime.UtcNow,
            Type = input.Type,
            Content = input.Content,
            Tool = input.Tool,
            Integration = input.Integration,
            Channel = input.Channel,
            CorrelationId = input.CorrelationId,
        };
        var saved = await logs.AppendAsync(record, ct);
        return AgentLogMapper.ToProjection(saved);
    }
}

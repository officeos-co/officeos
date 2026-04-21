namespace EnterpriseAgentOs.Api.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentLogsMutations
{
    public async Task<AgentLogDto> SendAgentMessage(
        Guid agentId,
        string content,
        IResolverContext context,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Message content must not be empty.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        var saved = await logs.SendMessageAsync(agentId, content, user.Id, ct);
        return AgentLogMapper.ToDto(saved);
    }

    public async Task<AgentLogDto> AppendAgentLog(
        AppendAgentLogInput input,
        IResolverContext context,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return AgentLogMapper.ToDto(saved);
    }
}

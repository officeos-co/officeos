namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentLogsMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto> SendAgentMessage(
        Guid agentId,
        string content,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Message content must not be empty.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }
        var saved = await logs.SendMessageAsync(agentId, content, user.Id, ct);
        return EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogMapper.ToDto(saved);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogDto> AppendAgentLog(
        EnterpriseAgentOs.Api.Entities.AgentLogs.AppendAgentLogInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService logs,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var record = new EnterpriseAgentOs.Api.Database.Models.AgentLogRecord
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
        return EnterpriseAgentOs.Api.Entities.AgentLogs.AgentLogMapper.ToDto(saved);
    }
}

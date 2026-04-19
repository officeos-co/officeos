namespace EnterpriseAgentOs.Api.GraphQL.SkillGateway;

/// <summary>
/// Root mutation type for the agent GraphQL schema.
/// Agent pods call these mutations to forward data back to the backend.
/// </summary>
public class AgentSchemaMutations
{
    /// <summary>
    /// Forwards a log entry from the agent pod to the backend timeline.
    /// </summary>
    public async Task<AgentLogDto> ForwardLog(
        ForwardLogInput input,
        IResolverContext context,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var http = context.Service<IHttpContextAccessor>().HttpContext!;
        var agentId = (Guid)http.Items["agent-id"]!;

        if (string.IsNullOrWhiteSpace(input.Content))
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Content must not be empty.")
                    .SetCode("BAD_INPUT")
                    .Build());

        if (!Enum.TryParse<AgentLogType>(input.Type, ignoreCase: true, out var logType))
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Unknown log type: {input.Type}")
                    .SetCode("BAD_INPUT")
                    .Build());

        var record = new AgentLogRecord
        {
            AgentId = agentId,
            Time = DateTime.UtcNow,
            Type = logType,
            Content = input.Content,
            CorrelationId = input.CorrelationId,
        };

        var saved = await logs.AppendAsync(record, ct);
        return AgentLogMapper.ToDto(saved);
    }
}

public sealed record ForwardLogInput(
    string Type,
    string Content,
    string? CorrelationId = null);

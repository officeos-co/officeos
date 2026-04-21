namespace EnterpriseAgentOs.Api.Agents;

/// <summary>
/// Agent-pod-facing REST endpoint for forwarding log entries.
/// Authenticated via <c>[AgentTokenAuth]</c> (bearer agent-uuid).
/// Agent pods call <c>POST /api/agents/me/logs</c> to sync messages,
/// tool calls, and system events back to the backend timeline.
/// </summary>
[ApiController]
[Route("api/agents/me/logs")]
[AgentTokenAuth]
public sealed class AgentLogController : ControllerBase
{
    private readonly IAgentLogService _agentLogService;

    public AgentLogController(IAgentLogService logs)
    {
        _agentLogService = logs;
    }

    public sealed record ForwardLogInput(
        string Type,
        string Content,
        string? CorrelationId = null);

    [HttpPost]
    public async Task<ActionResult<AgentLogDto>> Forward(
        [FromBody] ForwardLogInput input,
        CancellationToken ct)
    {
        var agentId = (Guid)HttpContext.Items["agent-id"]!;

        if (string.IsNullOrWhiteSpace(input.Content))
            return BadRequest("Content must not be empty.");

        if (!Enum.TryParse<AgentLogType>(input.Type, ignoreCase: true, out var logType))
            return BadRequest($"Unknown log type: {input.Type}");

        var record = new AgentLogRecord
        {
            AgentId = agentId,
            Time = DateTime.UtcNow,
            Type = logType,
            Content = input.Content,
            CorrelationId = input.CorrelationId,
        };

        var saved = await _agentLogService.AppendAsync(record, ct);
        return Ok(saved.ToDto());
    }
}

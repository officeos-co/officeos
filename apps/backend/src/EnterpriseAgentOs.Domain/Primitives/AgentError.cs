namespace EnterpriseAgentOs.Domain.Primitives;

/// <summary>
/// Structured agent error with category for dashboard filtering.
/// </summary>
public sealed record AgentError(
    AgentErrorCategory Category,
    string Message,
    string? Detail = null)
{
    public AgentLogType LogType => Category switch
    {
        AgentErrorCategory.PodConnection => AgentLogType.ErrorPodConnection,
        AgentErrorCategory.LlmCall => AgentLogType.ErrorLlmCall,
        AgentErrorCategory.ToolExecution => AgentLogType.ErrorToolExecution,
        AgentErrorCategory.SkillExecution => AgentLogType.ErrorSkillExecution,
        AgentErrorCategory.TurnOrchestration => AgentLogType.ErrorTurnOrchestration,
        AgentErrorCategory.Memory => AgentLogType.ErrorMemory,
        AgentErrorCategory.Configuration => AgentLogType.ErrorConfiguration,
        _ => AgentLogType.Error,
    };

    public string FormattedContent => Detail is not null
        ? $"{Category}: {Message}\n{Detail}"
        : $"{Category}: {Message}";

    /// <summary>
    /// Creates an AgentLogRecord from this error. Use this anywhere outside the turn loop
    /// where TurnLogger isn't available.
    /// </summary>
    public AgentLogRecord ToLogRecord(Guid agentId) => new()
    {
        AgentId = agentId,
        Type = LogType,
        Content = FormattedContent,
        Time = DateTime.UtcNow,
    };
}

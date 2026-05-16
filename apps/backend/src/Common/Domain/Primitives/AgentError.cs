using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Common.Domain.Primitives;

/// <summary>
/// Structured agent error with category for dashboard filtering.
/// </summary>
public sealed record AgentError(
    AgentErrorCategory Category,
    string Message,
    string? Detail = null)
{
    public ResourceLogType LogType => Category switch
    {
        AgentErrorCategory.PodConnection => ResourceLogType.ErrorPodConnection,
        AgentErrorCategory.LlmCall => ResourceLogType.ErrorLlmCall,
        AgentErrorCategory.ToolExecution => ResourceLogType.ErrorToolExecution,
        AgentErrorCategory.SkillExecution => ResourceLogType.ErrorSkillExecution,
        AgentErrorCategory.TurnOrchestration => ResourceLogType.ErrorTurnOrchestration,
        AgentErrorCategory.Memory => ResourceLogType.ErrorMemory,
        AgentErrorCategory.Configuration => ResourceLogType.ErrorConfiguration,
        _ => ResourceLogType.Error,
    };

    public string FormattedContent => Detail is not null
        ? $"{Category}: {Message}\n{Detail}"
        : $"{Category}: {Message}";

    /// <summary>
    /// Creates an ResourceLogRecord from this error. Use this anywhere outside the turn loop
    /// where TurnLogger isn't available.
    /// </summary>
    public ResourceLogRecord ToLogRecord(Guid agentId) => new()
    {
        AgentId = agentId,
        Type = LogType,
        Content = FormattedContent,
        Time = DateTime.UtcNow,
    };
}

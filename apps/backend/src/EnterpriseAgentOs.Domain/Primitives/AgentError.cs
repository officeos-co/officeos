namespace EnterpriseAgentOs.Domain.Primitives;

/// <summary>
/// Structured agent error with category for dashboard filtering.
/// </summary>
public sealed record AgentError(
    AgentErrorCategory Category,
    string Message,
    string? Detail = null);

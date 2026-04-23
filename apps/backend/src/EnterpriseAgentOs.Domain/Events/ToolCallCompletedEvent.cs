namespace EnterpriseAgentOs.Domain.Events;

public sealed record ToolCallCompletedEvent(Guid AgentId, string CorrelationId, string ToolName, bool Success, string Output, int DurationMs) : DomainEvent;

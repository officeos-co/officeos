namespace OffceOs.Domain.Events;

public sealed record ToolCallStartedEvent(Guid AgentId, string CorrelationId, string ToolName, string ArgsJson) : DomainEvent;

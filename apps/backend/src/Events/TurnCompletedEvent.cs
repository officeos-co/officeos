namespace OffceOs.Domain.Events;

public sealed record TurnCompletedEvent(Guid AgentId, string CorrelationId, int DurationMs, int Iterations, int ToolCallCount) : DomainEvent;

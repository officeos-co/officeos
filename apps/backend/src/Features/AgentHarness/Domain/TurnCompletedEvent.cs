namespace OffceOs.Domain.Features.AgentHarness;

public sealed record TurnCompletedEvent(Guid AgentId, string CorrelationId, int DurationMs, int Iterations, int ToolCallCount) : DomainEvent;

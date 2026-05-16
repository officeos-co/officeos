using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record TurnCompletedEvent(Guid AgentId, string CorrelationId, int DurationMs, int Iterations, int ToolCallCount) : DomainEvent;

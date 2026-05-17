using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record TurnCompletedEvent(Guid AgentId, Guid SessionId, string CorrelationId, int DurationMs, int Iterations, int ToolCallCount) : DomainEvent;

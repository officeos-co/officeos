using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record PodConnectedEvent(Guid AgentId, string CorrelationId, int DurationMs) : DomainEvent;

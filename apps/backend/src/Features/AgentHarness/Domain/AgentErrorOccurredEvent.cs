using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record AgentErrorOccurredEvent(Guid AgentId, string CorrelationId, string Message) : DomainEvent;

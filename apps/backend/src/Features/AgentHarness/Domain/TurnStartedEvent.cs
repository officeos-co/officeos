using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record TurnStartedEvent(Guid AgentId, string CorrelationId, string UserMessage) : DomainEvent;

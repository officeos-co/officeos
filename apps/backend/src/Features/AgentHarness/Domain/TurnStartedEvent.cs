using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record TurnStartedEvent(Guid AgentId, Guid SessionId, string CorrelationId, string UserMessage) : DomainEvent;

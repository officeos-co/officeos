using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record TurnStartedEvent(Guid AgentId, string CorrelationId, string UserMessage) : DomainEvent;

using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record MessageOutEvent(Guid AgentId, Guid SessionId, string CorrelationId, string Content) : DomainEvent;

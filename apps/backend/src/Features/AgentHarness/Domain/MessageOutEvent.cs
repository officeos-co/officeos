namespace OffceOs.Domain.Features.AgentHarness;

public sealed record MessageOutEvent(Guid AgentId, string CorrelationId, string Content) : DomainEvent;

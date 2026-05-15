namespace OffceOs.Domain.Features.Agents;

public sealed record AgentUpdatedEvent(Guid AgentId) : DomainEvent;

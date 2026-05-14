namespace OffceOs.Domain.Events;

public sealed record AgentCreatedEvent(Guid AgentId, string Provider, string? Model, Guid? OwnerId) : DomainEvent;

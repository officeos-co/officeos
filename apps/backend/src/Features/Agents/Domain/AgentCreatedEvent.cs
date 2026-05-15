namespace OffceOs.Domain.Features.Agents;

public sealed record AgentCreatedEvent(Guid AgentId, string Provider, string? Model, Guid? OwnerId) : DomainEvent;

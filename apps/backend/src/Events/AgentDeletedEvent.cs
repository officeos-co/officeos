namespace OffceOs.Domain.Events;

public sealed record AgentDeletedEvent(Guid AgentId, string? PodName, bool HasPod, Guid? OwnerId) : DomainEvent;

namespace OffceOs.Domain.Events;

public sealed record ChannelCredsStoredEvent(Guid ConnectionId) : DomainEvent;

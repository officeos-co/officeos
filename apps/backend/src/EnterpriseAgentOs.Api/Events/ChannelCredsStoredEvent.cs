namespace EnterpriseAgentOs.Domain.Events;

public sealed record ChannelCredsStoredEvent(Guid ConnectionId) : DomainEvent;

using OffceOs.Common.Domain;

namespace OffceOs.Features.Channels.Domain;

public sealed record ChannelCredsStoredEvent(Guid ConnectionId) : DomainEvent;

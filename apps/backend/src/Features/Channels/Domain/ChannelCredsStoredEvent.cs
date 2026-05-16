using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.Channels;

public sealed record ChannelCredsStoredEvent(Guid ConnectionId) : DomainEvent;

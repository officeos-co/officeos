using OffceOs.Database;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Observability;
using OffceOs.Infrastructure.Features.Observability;
using MediatR;

namespace OffceOs.Tests.Shared;

internal sealed class ChannelEventPersistingPublisher : IPublisher
{
    private readonly AgentLogRepository _agentLogRepository;

    public ChannelEventPersistingPublisher(EaosDbContext db)
    {
        _agentLogRepository = new AgentLogRepository(db);
    }

    public List<object> Notifications { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return PersistAsync(notification, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Notifications.Add(notification!);
        return PersistAsync(notification, cancellationToken);
    }

    private Task PersistAsync(object? notification, CancellationToken ct)
    {
        if (notification is not ChannelMessageRoutedEvent routed)
            return Task.CompletedTask;

        return _agentLogRepository.AppendAsync(new AgentLogRecord
        {
            AgentId = routed.AgentId,
            Type = routed.LogType,
            Channel = routed.ChannelType,
            ChannelConnectionId = routed.ChannelConnectionId,
            Content = routed.Content,
            CorrelationId = routed.CorrelationId,
        }, ct);
    }
}

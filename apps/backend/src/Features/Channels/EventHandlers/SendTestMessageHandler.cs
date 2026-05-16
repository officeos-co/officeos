using OffceOs.Features.Channels.Domain;
using OffceOs.Common.Application;
namespace OffceOs.Features.Channels.EventHandlers;

internal sealed class SendTestMessageHandler : INotificationHandler<ChannelCredsStoredEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SendTestMessageHandler(IServiceScopeFactory scopeFactory)
        => _serviceScopeFactory = scopeFactory;

    public Task Handle(ChannelCredsStoredEvent notification, CancellationToken ct)
    {
        BackgroundWork.Run<IChannelService>(
            _serviceScopeFactory,
            svc => svc.SendTestMessageAsync(notification.ConnectionId, CancellationToken.None),
            delay: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }
}

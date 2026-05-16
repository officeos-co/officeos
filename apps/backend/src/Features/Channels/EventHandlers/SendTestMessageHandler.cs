using OffceOs.Domain.Features.Channels;
using OffceOs.Application.Features;
namespace OffceOs.EventHandlers.Features.Channels;

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


namespace OffceOs.Tests.Shared;

public sealed class RecordingPublisher : IPublisher
{
    public List<object> Notifications { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }
}

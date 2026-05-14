namespace OffceOs.EventHandlers.Features.Observability;

internal sealed class PostHogAnalyticsHandler :
    INotificationHandler<AgentCreatedEvent>,
    INotificationHandler<AgentDeletedEvent>
{
    private readonly IPostHogService _postHogService;

    public PostHogAnalyticsHandler(IPostHogService postHog) => _postHogService = postHog;

    public async Task Handle(AgentCreatedEvent notification, CancellationToken ct)
    {
        if (notification.OwnerId is null) return;

        await _postHogService.CaptureAsync(
            notification.OwnerId.Value.ToString(),
            "agent_created",
            new Dictionary<string, object?>
            {
                ["agent_id"] = notification.AgentId,
                ["provider"] = notification.Provider,
                ["model"] = notification.Model,
            },
            ct);
    }

    public async Task Handle(AgentDeletedEvent notification, CancellationToken ct)
    {
        if (notification.OwnerId is null) return;

        await _postHogService.CaptureAsync(
            notification.OwnerId.Value.ToString(),
            "agent_deleted",
            new Dictionary<string, object?> { ["agent_id"] = notification.AgentId },
            ct);
    }
}

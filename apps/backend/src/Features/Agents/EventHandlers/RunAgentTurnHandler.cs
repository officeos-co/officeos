namespace OffceOs.EventHandlers.Features.Agents;

internal sealed class RunAgentTurnHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<RunAgentTurnHandler> _logger;

    public RunAgentTurnHandler(IServiceScopeFactory scopeFactory, ILogger<RunAgentTurnHandler> logger)
    {
        _serviceScopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(MessageReceivedEvent notification, CancellationToken ct)
    {
        BackgroundWork.Run<AgentTurnService>(
            _serviceScopeFactory,
            async svc =>
            {
                await svc.RunTurnAsync(
                    notification.AgentId, notification.Content,
                    notification.CorrelationId, CancellationToken.None);
            },
            _logger);

        return Task.CompletedTask;
    }
}

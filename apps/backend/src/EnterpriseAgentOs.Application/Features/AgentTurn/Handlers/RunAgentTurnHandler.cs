using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentTurn.Handlers;

internal sealed class RunAgentTurnHandler : INotificationHandler<MessageReceivedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RunAgentTurnHandler> _logger;

    public RunAgentTurnHandler(IServiceScopeFactory scopeFactory, ILogger<RunAgentTurnHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(MessageReceivedEvent notification, CancellationToken ct)
    {
        BackgroundWork.Run<AgentTurnService>(
            _scopeFactory,
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

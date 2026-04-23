using MediatR;

namespace EnterpriseAgentOs.Application.EventHandlers;

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
        if (string.IsNullOrEmpty(notification.PodName))
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var turnService = scope.ServiceProvider.GetRequiredService<AgentTurnService>();
                await turnService.RunTurnAsync(notification.AgentId, notification.Content, notification.CorrelationId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turn failed for agent {AgentId}", notification.AgentId);

                try
                {
                    using var errorScope = _scopeFactory.CreateScope();
                    var repo = errorScope.ServiceProvider.GetRequiredService<IAgentLogRepository>();
                    var sender = errorScope.ServiceProvider.GetRequiredService<ITopicEventSender>();

                    var errorRecord = await repo.AppendAsync(new AgentLogRecord
                    {
                        AgentId = notification.AgentId,
                        Type = AgentLogType.Error,
                        Content = $"Turn failed: {ex.Message}",
                        CorrelationId = notification.CorrelationId,
                        Time = DateTime.UtcNow,
                    });
                    await sender.SendAsync($"agent-log:{notification.AgentId}", errorRecord.ToDto(), CancellationToken.None);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log error for agent {AgentId}", notification.AgentId);
                }
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}

namespace OffceOs.Application.Features.Agents;

internal sealed class AgentWorkDispatchService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgentWorkDispatchService> _logger;

    public AgentWorkDispatchService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgentWorkDispatchService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchQueuedWorkAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queued agent work dispatch failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchQueuedWorkAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var agentLogService = scope.ServiceProvider.GetRequiredService<IAgentLogService>();
        var agentWorkExecutionService = scope.ServiceProvider.GetRequiredService<IAgentWorkExecutionService>();

        for (var i = 0; i < 10; i++)
        {
            var work = await agentLogService.ClaimNextQueuedWorkAsync(ct);
            if (work is null)
                return;

            await agentWorkExecutionService.ExecuteQueuedWorkAsync(work, ct);
        }
    }
}

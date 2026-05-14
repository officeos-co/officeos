namespace OffceOs.Application.Features.Agents;

internal sealed class AgentRunDispatchService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgentRunDispatchService> _logger;

    public AgentRunDispatchService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgentRunDispatchService> logger)
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
                await DispatchQueuedRunsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queued agent run dispatch failed");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchQueuedRunsAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var agentRunRepository = scope.ServiceProvider.GetRequiredService<IAgentRunRepository>();
        var controlPlaneRuns = scope.ServiceProvider.GetRequiredService<IControlPlaneRunService>();

        var queuedRuns = await agentRunRepository.ListAsync(new AgentRunFilter
        {
            Kind = "opencode",
            Status = "queued",
        }, 10, ct);

        foreach (var run in queuedRuns.OrderBy(run => run.CreatedAt))
            await controlPlaneRuns.ExecuteQueuedRunAsync(run, ct);
    }
}

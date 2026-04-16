namespace EnterpriseAgentOs.Api.Entities.Runners;

/// <summary>
/// Background service that runs every 30s to:
/// 1. Fail stale pending/running jobs
/// 2. Mark runners as offline if heartbeat is stale (>90s)
/// </summary>
public sealed class RunnerJobTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RunnerJobWaiter _waiter;
    private readonly ILogger<RunnerJobTimeoutService> _logger;

    public RunnerJobTimeoutService(
        IServiceScopeFactory scopeFactory,
        RunnerJobWaiter waiter,
        ILogger<RunnerJobTimeoutService> logger)
    {
        _scopeFactory = scopeFactory;
        _waiter = waiter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseAgentOs.Api.Database.EaosDbContext>();
                var jobRepo = scope.ServiceProvider.GetRequiredService<IRunnerJobRepository>();

                // Fail stale jobs
                await jobRepo.FailStaleJobsAsync(stoppingToken);

                // Mark runners offline if heartbeat is stale
                var cutoff = DateTime.UtcNow.AddSeconds(-90);
                var staleRunners = await db.Runners
                    .Where(r => r.Status == "online" && r.LastHeartbeatAt != null && r.LastHeartbeatAt < cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var runner in staleRunners)
                {
                    runner.Status = "offline";
                    _logger.LogInformation("Runner {RunnerId} marked offline (stale heartbeat)", runner.Id);
                }

                if (staleRunners.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in runner job timeout service");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

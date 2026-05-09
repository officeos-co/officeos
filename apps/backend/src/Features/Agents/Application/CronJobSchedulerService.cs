namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Background service that ticks every 30 seconds, evaluates all enabled cron jobs,
/// and fires agent turns for any that are due.
/// </summary>
internal sealed class CronJobSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CronJobSchedulerService> _logger;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    public CronJobSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<CronJobSchedulerService> logger)
    {
        _serviceScopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CronJobSchedulerService started");

        // Wait a bit on startup for the rest of the app to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "CronJobScheduler tick failed");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cronRepo = scope.ServiceProvider.GetRequiredService<IAgentCronJobRepository>();
        var logService = scope.ServiceProvider.GetRequiredService<IAgentLogService>();

        var allJobs = await cronRepo.ListAllEnabledAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var job in allJobs)
        {
            try
            {
                var cron = Cronos.CronExpression.Parse(job.Expression);

                // If NextRunAt is not set, compute it now and skip this tick
                if (job.NextRunAt is null)
                {
                    var next = cron.GetNextOccurrence(now, inclusive: false);
                    if (next.HasValue)
                    {
                        job.SetNextRun(next.Value);
                        await cronRepo.UpdateAsync(job, ct);
                    }
                    continue;
                }

                // Is it time to run?
                if (job.NextRunAt > now) continue;

                _logger.LogInformation("Firing cron job {JobId} '{JobName}' for agent {AgentId}",
                    job.Id, job.Name, job.AgentId);

                // Fire the agent turn — SendMessageAsync handles the async turn dispatch
                var prompt = $"[Scheduled task: {job.Name}]\n\n{job.Prompt}";
                await logService.SendMessageAsync(job.AgentId, prompt, userId: Guid.Empty, ct);

                // Update last run + compute next run
                job.UpdateLastRun(now);
                var nextRun = cron.GetNextOccurrence(now, inclusive: false);
                job.SetNextRun(nextRun);
                await cronRepo.UpdateAsync(job, ct);
            }
            catch (Cronos.CronFormatException ex)
            {
                _logger.LogWarning(ex, "Invalid cron expression for job {JobId}: {Expression}",
                    job.Id, job.Expression);
                var error = new AgentError(AgentErrorCategory.Configuration, $"Invalid cron expression: {job.Expression}", ex.Message);
                await logService.AppendAsync(error.ToLogRecord(job.AgentId), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fire cron job {JobId} for agent {AgentId}",
                    job.Id, job.AgentId);
                var error = new AgentError(AgentErrorCategory.TurnOrchestration, $"Cron job '{job.Name}' failed: {ex.Message}", ex.ToString());
                await logService.AppendAsync(error.ToLogRecord(job.AgentId), ct);
            }
        }
    }
}

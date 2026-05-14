namespace OffceOs.Application.Features.AgentRoutines;

internal sealed class AgentRoutineSchedulerService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AgentRoutineSchedulerService> _logger;

    public AgentRoutineSchedulerService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AgentRoutineSchedulerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentRoutineSchedulerService started");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var executionService = scope.ServiceProvider.GetRequiredService<IAgentRoutineExecutionService>();
                await executionService.RunDueSchedulesAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Agent routine scheduler tick failed");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }
}

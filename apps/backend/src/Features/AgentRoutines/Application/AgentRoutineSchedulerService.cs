using OffceOs.Features.ResourceLogs.Application;

namespace OffceOs.Features.AgentRoutines.Application;

internal sealed class AgentRoutineSchedulerService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AgentRoutineSchedulerService(IServiceScopeFactory serviceScopeFactory)
        => _serviceScopeFactory = serviceScopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                using var scope = _serviceScopeFactory.CreateScope();
                var resourceLogWriterService = scope.ServiceProvider.GetRequiredService<IResourceLogWriterService>();
                await resourceLogWriterService
                    .ForControlPlane()
                    .ErrorAsync(ex, "Agent routine scheduler tick failed", stoppingToken);
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }
}

using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.AgentHarness.Application;

internal sealed class AgentRuntimeCleanupService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AgentRuntimeCleanupService(IServiceScopeFactory scopeFactory)
        => _serviceScopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var resourceLogWriterService = scope.ServiceProvider.GetRequiredService<IResourceLogWriterService>();
                await resourceLogWriterService
                    .ForControlPlane()
                    .ErrorAsync(ex, "Agent runtime cleanup tick failed", stoppingToken);
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var agentSessionRepository = scope.ServiceProvider.GetRequiredService<IAgentSessionRepository>();
        var cleaner = scope.ServiceProvider.GetRequiredService<IAgentRuntimeCleaner>();

        var activeSessionIds = (await agentSessionRepository.ListAsync(new AgentSessionFilter { Status = SessionStatus.Running }, 500, ct))
            .Select(session => session.Id)
            .ToHashSet();

        var result = await cleaner.CleanupUnusedAsync(activeSessionIds, ct);
        if (result.Pods == 0 && result.Services == 0 && result.Volumes == 0)
            return;

        var resourceLogWriterService = scope.ServiceProvider.GetRequiredService<IResourceLogWriterService>();
        await resourceLogWriterService
            .ForControlPlane()
            .InfoAsync(
                "Cleaned unused agent runtimes: {Pods} pods or containers, {Services} services, {Volumes} volumes or PVCs",
                [result.Pods, result.Services, result.Volumes],
                ct);
    }
}

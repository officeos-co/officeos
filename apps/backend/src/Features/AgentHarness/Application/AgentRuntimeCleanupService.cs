using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Features.Agents;
namespace OffceOs.Application.Features.AgentHarness;

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
        var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
        var cleaner = scope.ServiceProvider.GetRequiredService<IAgentRuntimeCleaner>();

        var activeAgentIds = (await agentRepository.ListAsync(new AgentFilter(), ct))
            .Select(agent => agent.Id)
            .ToHashSet();

        var result = await cleaner.CleanupUnusedAsync(activeAgentIds, ct);
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

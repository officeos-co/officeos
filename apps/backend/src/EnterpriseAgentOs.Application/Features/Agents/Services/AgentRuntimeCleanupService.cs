using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentRuntimeCleanupService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentRuntimeCleanupService> _logger;

    public AgentRuntimeCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentRuntimeCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentRuntimeCleanupService started");
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Agent runtime cleanup tick failed");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
        var cleaner = scope.ServiceProvider.GetRequiredService<IAgentRuntimeCleaner>();

        var activeAgentIds = (await agentRepository.ListAsync(new AgentFilter(), ct))
            .Select(agent => agent.Id)
            .ToHashSet();

        var result = await cleaner.CleanupUnusedAsync(activeAgentIds, ct);
        if (result.Pods > 0 || result.Services > 0 || result.Volumes > 0)
        {
            _logger.LogInformation(
                "Cleaned unused agent runtimes: {Pods} pods/containers, {Services} services, {Volumes} volumes/PVCs",
                result.Pods,
                result.Services,
                result.Volumes);
        }
    }
}

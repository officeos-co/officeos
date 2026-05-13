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
        var agentTurnService = scope.ServiceProvider.GetRequiredService<AgentTurnService>();

        var queuedRuns = new List<AgentRunRecord>();
        queuedRuns.AddRange(await agentRunRepository.ListAsync(new AgentRunFilter
        {
            Kind = "subagent",
            Status = "queued",
        }, 10, ct));
        queuedRuns.AddRange(await agentRunRepository.ListAsync(new AgentRunFilter
        {
            Kind = "fork",
            Status = "queued",
        }, 10, ct));

        foreach (var run in queuedRuns.OrderBy(run => run.CreatedAt))
            await DispatchRunAsync(agentRunRepository, agentTurnService, run, ct);
    }

    private static async Task DispatchRunAsync(
        IAgentRunRepository agentRunRepository,
        AgentTurnService agentTurnService,
        AgentRunRecord run,
        CancellationToken ct)
    {
        run.Status = "running";
        run.UpdatedAt = DateTime.UtcNow;
        await agentRunRepository.UpdateAsync(run, ct);

        var correlationId = run.ParentCorrelationId ?? Guid.NewGuid().ToString("N");
        using var ambientRun = AgentRunContext.Begin(run.Id, run.ParentRunId);
        var result = await agentTurnService.RunTurnAsync(run.AgentId, run.Prompt, correlationId, ct);

        run.Status = result.Success ? "completed" : "failed";
        run.Result = result.Content;
        run.Error = result.Error;
        run.CompletedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await agentRunRepository.UpdateAsync(run, ct);
    }
}

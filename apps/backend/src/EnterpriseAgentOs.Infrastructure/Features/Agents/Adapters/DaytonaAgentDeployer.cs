namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class DaytonaAgentDeployer : IAgentDeployer
{
    private readonly IAgentSandbox _sandbox;
    private readonly ILogger<DaytonaAgentDeployer> _logger;

    public DaytonaAgentDeployer(IAgentSandbox sandbox, ILogger<DaytonaAgentDeployer> logger)
    {
        _sandbox = sandbox;
        _logger = logger;
    }

    public async Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default)
    {
        var metadata = new Dictionary<string, string>
        {
            ["eaos.agent_id"] = agentId.ToString(),
            ["eaos.capabilities"] = "shell,file_read,file_write,file_edit,content_search,glob_search",
        };

        var deployment = await _sandbox.CreateAsync(agentId, null, new Dictionary<string, string>(), metadata, ct);
        return new AgentDeployment(deployment.SandboxId, deployment.ServiceUrl ?? string.Empty);
    }

    public Task<bool> RemoveAsync(string podName, CancellationToken ct = default)
        => _sandbox.TerminateAsync(podName, ct);

    public Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
        => Task.FromResult("unknown");

    public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default)
    {
        _logger.LogDebug("Daytona logs are not exposed through IAgentDeployer for sandbox {SandboxId}", podName);
        return Task.FromResult(string.Empty);
    }
}

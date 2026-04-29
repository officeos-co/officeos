namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

/// <summary>
/// No-op deployer used when Kubernetes is disabled (local development).
/// All operations return safe defaults without hitting any cluster API.
/// </summary>
internal sealed class NullAgentDeployer : IAgentDeployer
{
    private readonly ILogger<NullAgentDeployer> _logger;

    public NullAgentDeployer(ILogger<NullAgentDeployer> logger)
    {
        _logger = logger;
    }

    public Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default)
    {
        _logger.LogWarning("Kubernetes is disabled — cannot deploy agent {AgentId}", agentId);
        throw new InvalidOperationException(
            "Agent deployment is not available in this environment. Kubernetes is disabled.");
    }

    public Task<bool> RemoveAsync(string podName, CancellationToken ct = default)
    {
        _logger.LogDebug("Kubernetes is disabled — skipping remove for {Pod}", podName);
        return Task.FromResult(true);
    }

    public Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        return Task.FromResult("not_available");
    }

    public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default)
    {
        return Task.FromResult(string.Empty);
    }
}

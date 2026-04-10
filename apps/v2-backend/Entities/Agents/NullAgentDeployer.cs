namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class NullAgentDeployer : IAgentDeployer
{
    public Task<AgentDeployment> DeployAsync(
        Guid agentId,
        string provider,
        string apiKey,
        string? model,
        string backendToken,
        CancellationToken ct = default)
    {
        var podName = $"zeroclaw-{agentId.ToString("N").Substring(0, 8)}";
        var serviceUrl = $"ws://{podName}.default.svc.cluster.local:42617/ws/chat";
        return Task.FromResult(new AgentDeployment(podName, serviceUrl));
    }

    public Task<bool> RemoveAsync(string podName, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<string> GetStatusAsync(string podName, CancellationToken ct = default) =>
        Task.FromResult("running");

    public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default) =>
        Task.FromResult("[null-deployer] no logs\n");
}

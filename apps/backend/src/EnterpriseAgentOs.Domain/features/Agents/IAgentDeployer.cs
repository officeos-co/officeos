namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentDeployment(string PodName, string ServiceUrl);

public interface IAgentDeployer
{
    Task<AgentDeployment> DeployAsync(
        Guid agentId,
        CancellationToken ct = default);

    Task<bool> RemoveAsync(string podName, CancellationToken ct = default);

    Task<string> GetStatusAsync(string podName, CancellationToken ct = default);

    Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default);
}

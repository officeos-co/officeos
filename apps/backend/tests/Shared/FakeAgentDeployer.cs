using OffceOs.Domain.Features.Agents;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentDeployer : IAgentDeployer
{
    public Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult(new AgentDeployment($"agent-{agentId:N}", "http://agent"));

    public Task<bool> RemoveAsync(string podName, CancellationToken ct = default) => Task.FromResult(true);

    public Task<string> GetStatusAsync(string podName, CancellationToken ct = default) => Task.FromResult("running");

    public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default) =>
        Task.FromResult(string.Empty);
}

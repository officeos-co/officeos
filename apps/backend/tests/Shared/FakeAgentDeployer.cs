using OffceOs.Features.AgentHarness.Domain;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentDeployer : IAgentDeployer
{
    public string Status { get; set; } = "running";

    public Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult(new AgentDeployment($"agent-{agentId:N}", "http://agent"));

    public Task<bool> RemoveAsync(string podName, CancellationToken ct = default) => Task.FromResult(true);

    public Task<string> GetStatusAsync(string podName, CancellationToken ct = default) => Task.FromResult(Status);

    public Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default) =>
        Task.FromResult(string.Empty);
}

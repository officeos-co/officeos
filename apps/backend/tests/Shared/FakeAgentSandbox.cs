using OffceOs.Common.Domain.Primitives;
using OffceOs.Features.AgentHarness.Domain;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentSandbox : IAgentSandbox
{
    private readonly AgentSandboxCommandResult _result;

    public FakeAgentSandbox()
        : this(new AgentSandboxCommandResult(string.Empty, 0))
    {
    }

    public FakeAgentSandbox(AgentSandboxCommandResult result)
    {
        _result = result;
    }

    public List<(string SandboxId, string ServiceUrl, string Command, TimeSpan Timeout)> Executions { get; } = [];

    public Task<AgentSandboxDeployment> CreateAsync(
        Guid agentId,
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default) =>
        Task.FromResult(new AgentSandboxDeployment("sandbox-1", "http://sandbox"));

    public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        Executions.Add((sandboxId, serviceUrl, command, timeout));
        return Task.FromResult<AgentResult<AgentSandboxCommandResult>>(_result);
    }

    public Task<AgentResult<string>> ReadFileAsync(string sandboxId, string serviceUrl, string path, CancellationToken ct = default) =>
        Task.FromResult<AgentResult<string>>(string.Empty);

    public Task<AgentResult<bool>> WriteFileAsync(string sandboxId, string serviceUrl, string path, string content, CancellationToken ct = default) =>
        Task.FromResult<AgentResult<bool>>(true);

    public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default) => Task.FromResult(true);
}

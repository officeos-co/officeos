using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Sandbox;

public sealed class ToolSandboxTests
{
    [Fact]
    public async Task ShellTool_executes_through_agent_sandbox()
    {
        var sandbox = new FakeAgentSandbox(new AgentSandboxCommandResult("ok", 0));
        var context = new ToolExecutionContext(Guid.NewGuid(), "sandbox-1", "http://toolbox.local/toolbox/sandbox-1", sandbox);
        var tool = new ShellTool(context);

        var args = JsonSerializer.SerializeToElement(new { command = "echo ok", timeout_secs = 5 });
        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("ok", result.Value.Output);
        Assert.Equal("sandbox-1", sandbox.Executions.Single().SandboxId);
        Assert.Equal("echo ok", sandbox.Executions.Single().Command);
    }

    private sealed class FakeAgentSandbox : IAgentSandbox
    {
        private readonly AgentSandboxCommandResult _result;

        public FakeAgentSandbox(AgentSandboxCommandResult result)
        {
            _result = result;
        }

        public List<(string SandboxId, string ServiceUrl, string Command, TimeSpan Timeout)> Executions { get; } = [];

        public Task<AgentSandboxDeployment> CreateAsync(
            Guid agentId,
            IReadOnlyDictionary<string, string> environment,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken ct = default)
            => Task.FromResult(new AgentSandboxDeployment("sandbox-1", null));

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

        public Task<AgentResult<string>> ReadFileAsync(string sandboxId, string serviceUrl, string path, CancellationToken ct = default)
            => Task.FromResult<AgentResult<string>>(string.Empty);

        public Task<AgentResult<bool>> WriteFileAsync(string sandboxId, string serviceUrl, string path, string content, CancellationToken ct = default)
            => Task.FromResult<AgentResult<bool>>(true);

        public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}

using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Features.Agents;
using OffceOs.Tests.Shared;
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

}

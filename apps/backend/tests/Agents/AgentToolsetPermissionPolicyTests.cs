using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Features.AgentDefinitions;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentToolsetPermissionPolicyTests
{
    [Fact]
    public void Always_allow_builtin_toolset_allows_builtin_tools()
    {
        var policy = new AgentToolsetPermissionPolicy(Config(new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null)));

        Assert.True(policy.IsAllowed(new StubTool("shell", AgentToolKind.Execute)));
        Assert.True(policy.IsAllowed(new StubTool("file_read", AgentToolKind.Read)));
    }

    [Fact]
    public void Allow_list_builtin_toolset_exposes_only_listed_tools()
    {
        var policy = new AgentToolsetPermissionPolicy(Config(new AgentToolPermissionConfig(
            AgentToolPermissionKinds.AllowList,
            ["file_read"])));

        Assert.True(policy.IsAllowed(new StubTool("file_read", AgentToolKind.Read)));
        Assert.False(policy.IsAllowed(new StubTool("shell", AgentToolKind.Execute)));
    }

    [Fact]
    public void Deny_list_mcp_toolset_removes_only_listed_tools()
    {
        var config = new AgentDefinitionConfig(
            "Support agent",
            null,
            "gpt-4o-mini",
            null,
            [new AgentMcpServerConfig("notion", "registered", null)],
            [new AgentToolsetConfig(
                AgentToolsetKinds.Mcp,
                "notion",
                new AgentToolsetDefaultConfig(new AgentToolPermissionConfig(AgentToolPermissionKinds.DenyList, ["delete_page"])))],
            null,
            null,
            null);
        var policy = new AgentToolsetPermissionPolicy(config);

        Assert.True(policy.IsAllowed(new StubTool("notion__search", AgentToolKind.Integration)));
        Assert.False(policy.IsAllowed(new StubTool("notion__delete_page", AgentToolKind.Integration)));
    }

    private static AgentDefinitionConfig Config(AgentToolPermissionConfig permissionConfig)
        => new(
            "Agent",
            null,
            "gpt-4o-mini",
            null,
            [],
            [new AgentToolsetConfig(AgentToolsetKinds.Builtin, null, new AgentToolsetDefaultConfig(permissionConfig))],
            null,
            null,
            null);

    private sealed class StubTool : IAgentTool
    {
        public StubTool(string name, AgentToolKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public string Name { get; }
        public AgentToolKind Kind { get; }
        public ToolSchema Schema => new(Name, Name, new { type = "object", properties = new { } });

        public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
            => Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, string.Empty));
    }
}

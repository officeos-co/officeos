using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.AgentHarness;
using Xunit;

namespace OffceOs.Tests.AgentHarness;

public sealed class AgentHarnessToolPermissionResolverTests
{
    [Fact]
    public void Resolve_allows_only_listed_builtin_tools_plus_self_management_tools()
    {
        var permissions = Resolve(
            """
            name: Support agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
                default_config:
                  permission_policy:
                    type: allow_list
                    tools:
                      - memory_recall
                      - task_create
                      - task_list
                      - task_update
            """);

        Assert.False(permissions.InternalChannelSend);
        Assert.True(permissions.MemoryRecall);
        Assert.True(permissions.TaskCreate);
        Assert.True(permissions.TaskList);
        Assert.True(permissions.TaskUpdate);
        Assert.True(permissions.RoutineCreate);
        Assert.True(permissions.RoutineList);
        Assert.True(permissions.RoutineDelete);
        Assert.False(permissions.Shell);
        Assert.False(permissions.TaskGet);
        Assert.Contains("shell", permissions.DeniedBuiltinToolNames);
        Assert.DoesNotContain("memory_recall", permissions.DeniedBuiltinToolNames);
    }

    [Fact]
    public void Resolve_accepts_scoped_builtin_tool_names()
    {
        var permissions = Resolve(
            """
            name: Support agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
                default_config:
                  permission_policy:
                    type: allow_list
                    tools:
                      - builtin:memory_recall
            """);

        Assert.True(permissions.MemoryRecall);
        Assert.False(permissions.MemoryStore);
    }

    [Fact]
    public void Resolve_deny_list_excludes_matching_builtin_tools()
    {
        var permissions = Resolve(
            """
            name: Support agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
                default_config:
                  permission_policy:
                    type: deny_list
                    tools:
                      - shell
                      - http_request
            """);

        Assert.False(permissions.Shell);
        Assert.False(permissions.HttpRequest);
        Assert.True(permissions.FileRead);
        Assert.True(permissions.MemoryRecall);
    }

    [Fact]
    public void Resolve_always_deny_keeps_current_self_management_defaults()
    {
        var permissions = Resolve(
            """
            name: Support agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
                default_config:
                  permission_policy:
                    type: always_deny
            """);

        Assert.False(permissions.Shell);
        Assert.False(permissions.MemoryRecall);
        Assert.True(permissions.ToolSearch);
        Assert.True(permissions.RoutineCreate);
        Assert.True(permissions.RoutineList);
        Assert.True(permissions.RoutineDelete);
    }

    [Fact]
    public void Resolve_applies_browser_tool_policy_to_individual_browser_tools()
    {
        var permissions = Resolve(
            """
            name: Browser agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
              - type: browser_toolset
                default_config:
                  permission_policy:
                    type: allow_list
                    tools:
                      - browser.screenshot
                      - browser:navigate
            """);

        Assert.True(permissions.Browser);
        Assert.True(permissions.BrowserScreenshot);
        Assert.True(permissions.BrowserNavigate);
        Assert.False(permissions.BrowserEvalJs);
        Assert.Contains("browser__eval_js", permissions.DeniedBrowserToolNames);
        Assert.DoesNotContain("browser__screenshot", permissions.DeniedBrowserToolNames);
    }

    [Fact]
    public void Resolve_uses_channel_permission_input_for_channel_tools()
    {
        var permissions = Resolve(
            """
            name: Channel agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
                default_config:
                  permission_policy:
                    type: always_allow
            """,
            canSendInternalChannel: true);

        Assert.True(permissions.InternalChannelSend);
        Assert.DoesNotContain("internal_channel_send", permissions.DeniedChannelToolNames);
    }

    [Fact]
    public void ChannelPolicyAllows_reads_channel_owned_permission_policy()
    {
        var policy = new AgentHarnessToolPermissionResolver();
        var policyJson = """
            {
              "type": "allow_list",
              "tools": ["internal_channel_send"]
            }
            """;

        Assert.True(policy.ChannelPolicyAllows(policyJson, "internal_channel_send"));
        Assert.False(policy.ChannelPolicyAllows(policyJson, "other_channel_tool"));
    }

    private static AgentHarnessResolvedToolPolicy Resolve(string config, bool canSendInternalChannel = false)
    {
        var parser = new AgentDefinitionParser();
        var definitionConfig = parser.Parse(config);
        return new AgentHarnessToolPermissionResolver().Resolve(definitionConfig, canSendInternalChannel);
    }
}

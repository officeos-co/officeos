using OffceOs.Configuration;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.AgentHarness.Application.Tools;
namespace OffceOs.Features.AgentHarness.Application;

internal sealed class AgentHarnessToolPermissionPolicy
{
    private readonly AgentHarnessToolPermissionConfig _agentHarnessToolPermissionConfig;

    public AgentHarnessToolPermissionPolicy(AgentHarnessToolPermissionConfig agentHarnessToolPermissionConfig)
    {
        _agentHarnessToolPermissionConfig = agentHarnessToolPermissionConfig;
    }

    public bool AlwaysLoad(IAgentTool tool)
        => !ShouldDefer(tool);

    public bool ShouldDefer(IAgentTool tool)
    {
        if (tool.ShouldDeferOverride is { } overrideValue)
            return overrideValue;

        if (_agentHarnessToolPermissionConfig.DeferIntegrationTools && tool.Kind is AgentToolKind.Integration)
            return true;

        if (_agentHarnessToolPermissionConfig.DeferredToolNamePrefixes.Any(prefix =>
                tool.Name.StartsWith(prefix, StringComparison.Ordinal)))
            return true;

        return _agentHarnessToolPermissionConfig.DeferUnknownBuiltinTools
            && !_agentHarnessToolPermissionConfig.EagerToolNames.Contains(tool.Name);
    }

    public string ScopeFor(IAgentTool tool)
    {
        if (!string.IsNullOrWhiteSpace(tool.PermissionScopeOverride))
            return tool.PermissionScopeOverride;

        if (tool.Name.StartsWith("browser__", StringComparison.Ordinal))
            return $"browser:{tool.Name}";

        if (tool.Name.Contains("__", StringComparison.Ordinal))
        {
            var parts = tool.Name.Split("__", 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? $"{parts[0]}:{parts[1]}" : tool.Name;
        }

        return $"builtin:{tool.Name}";
    }

    public string GroupFor(IAgentTool tool)
    {
        if (tool.Kind == AgentToolKind.Integration)
            return ToolKey.Parse(ScopeFor(tool)).SkillName;

        return tool.Name.StartsWith("browser__", StringComparison.Ordinal)
            ? "browser"
            : "builtin";
    }
}

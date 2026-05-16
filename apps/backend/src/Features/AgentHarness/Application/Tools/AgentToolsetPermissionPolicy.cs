using OffceOs.Domain.Features.AgentDefinitions;
using OffceOs.Domain.Common.ValueObjects;
namespace OffceOs.Application.Features.AgentHarness;

internal sealed class AgentToolsetPermissionPolicy
{
    private readonly AgentDefinitionConfig _agentDefinitionConfig;
    private readonly AgentHarnessToolPermissionPolicy _agentHarnessToolPermissionPolicy;

    public AgentToolsetPermissionPolicy(
        AgentDefinitionConfig agentDefinitionConfig,
        AgentHarnessToolPermissionPolicy agentHarnessToolPermissionPolicy)
    {
        _agentDefinitionConfig = agentDefinitionConfig;
        _agentHarnessToolPermissionPolicy = agentHarnessToolPermissionPolicy;
    }

    public bool IsAllowed(IAgentTool tool)
    {
        var matchingToolsets = MatchingToolsets(tool).ToList();
        return matchingToolsets.Count > 0 && matchingToolsets.Any(toolset => PolicyAllows(tool, toolset));
    }

    public bool AllowsIntegrationTool(string integrationName, string toolName)
        => _agentDefinitionConfig.Tools
            .Where(toolset => toolset.Type == AgentToolsetKinds.Mcp
                && string.Equals(toolset.McpServerName, integrationName, StringComparison.OrdinalIgnoreCase))
            .Any(toolset => PolicyAllows(integrationName, toolName, $"{Slug(integrationName)}__{Slug(toolName)}", toolset));

    private IEnumerable<AgentToolsetConfig> MatchingToolsets(IAgentTool tool)
    {
        var key = ToolKey.Parse(_agentHarnessToolPermissionPolicy.ScopeFor(tool));
        if (tool.Name.StartsWith("browser__", StringComparison.Ordinal))
            return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Browser);

        if (tool.Kind == AgentToolKind.Integration)
            return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Mcp
                && string.Equals(toolset.McpServerName, key.SkillName, StringComparison.OrdinalIgnoreCase));

        return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Builtin);
    }

    private bool PolicyAllows(IAgentTool tool, AgentToolsetConfig toolset)
    {
        var key = ToolKey.Parse(_agentHarnessToolPermissionPolicy.ScopeFor(tool));
        return PolicyAllows(key.SkillName, key.ToolName, tool.Name, toolset);
    }

    private static bool PolicyAllows(string groupName, string toolName, string runtimeName, AgentToolsetConfig toolset)
    {
        var policy = toolset.DefaultConfig?.PermissionPolicy
            ?? new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null);

        return policy.Type switch
        {
            AgentToolPermissionKinds.AlwaysAllow => true,
            AgentToolPermissionKinds.AlwaysDeny => false,
            AgentToolPermissionKinds.AllowList => Matches(policy.Tools, groupName, toolName, runtimeName),
            AgentToolPermissionKinds.DenyList => !Matches(policy.Tools, groupName, toolName, runtimeName),
            _ => false,
        };
    }

    private static bool Matches(IReadOnlyList<string>? patterns, string groupName, string toolName, string runtimeName)
    {
        if (patterns is null)
            return false;

        var scope = $"{groupName}:{toolName}";
        return patterns.Any(pattern =>
            string.Equals(pattern, toolName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pattern, runtimeName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pattern, scope, StringComparison.OrdinalIgnoreCase));
    }

    private static string Slug(string value)
        => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

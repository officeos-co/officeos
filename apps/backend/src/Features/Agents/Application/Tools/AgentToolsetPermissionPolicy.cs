namespace OffceOs.Application.Features.Agents;

internal sealed class AgentToolsetPermissionPolicy
{
    private readonly AgentDefinitionConfig _agentDefinitionConfig;

    public AgentToolsetPermissionPolicy(AgentDefinitionConfig agentDefinitionConfig)
    {
        _agentDefinitionConfig = agentDefinitionConfig;
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

    public static string? GetOrganizationPolicyDenialReason(IAgentTool tool, OrganizationPolicyProfileRecord policy)
    {
        var key = ToolKey.Parse(tool.PermissionScope);
        var scope = $"{key.SkillName}:{key.ToolName}";
        var deniedTools = ParseStringSet(policy.DeniedToolsJson);
        var allowedTools = ParseStringSet(policy.AllowedToolsJson);
        var deniedIntegrations = ParseStringSet(policy.DeniedIntegrationsJson);
        var allowedIntegrations = ParseStringSet(policy.AllowedIntegrationsJson);

        if (tool.Name.StartsWith("browser__", StringComparison.Ordinal) && !policy.BrowserToolsEnabled)
            return "browser tools are disabled by organization policy";

        if (tool.Kind is AgentToolKind.Network && !policy.NetworkToolsEnabled)
            return "network tools are disabled by organization policy";

        if (tool.Name.Equals("shell", StringComparison.Ordinal) && !policy.ShellToolsEnabled)
            return "shell tools are disabled by organization policy";

        if (tool.Name is "file_write" or "file_edit" && !policy.FileWriteToolsEnabled)
            return "file write tools are disabled by organization policy";

        if (deniedTools.Contains(scope) || deniedTools.Contains($"{key.SkillName}:") || deniedTools.Contains(tool.Name))
            return "tool is denied by organization policy";

        if (tool.Kind is AgentToolKind.Integration)
        {
            if (deniedIntegrations.Contains(key.SkillName))
                return "integration is denied by organization policy";
            if (allowedIntegrations.Count > 0 && !allowedIntegrations.Contains(key.SkillName))
                return "integration is not allowed by organization policy";
        }

        return allowedTools.Count == 0 || allowedTools.Contains(scope) || allowedTools.Contains($"{key.SkillName}:") || allowedTools.Contains(tool.Name)
            ? null
            : "tool is not allowed by organization policy";
    }

    private IEnumerable<AgentToolsetConfig> MatchingToolsets(IAgentTool tool)
    {
        var key = ToolKey.Parse(tool.PermissionScope);
        if (tool.Name.StartsWith("browser__", StringComparison.Ordinal))
            return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Browser);

        if (tool.Kind == AgentToolKind.Integration)
            return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Mcp
                && string.Equals(toolset.McpServerName, key.SkillName, StringComparison.OrdinalIgnoreCase));

        return _agentDefinitionConfig.Tools.Where(toolset => toolset.Type == AgentToolsetKinds.Builtin);
    }

    private static bool PolicyAllows(IAgentTool tool, AgentToolsetConfig toolset)
    {
        var key = ToolKey.Parse(tool.PermissionScope);
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

    private static HashSet<string> ParseStringSet(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray()
                    .Select(value => value.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string Slug(string value)
        => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

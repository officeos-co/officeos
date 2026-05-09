namespace OffceOs.Application.Features.Agents;

internal sealed class AgentToolPermissionResolver
{
    private readonly Dictionary<string, ToolPermission> _permissions;

    public AgentToolPermissionResolver(IEnumerable<AgentToolPermissionRecord> permissions)
    {
        _permissions = permissions.ToDictionary(
            p => Key(p.SkillName, p.ToolName),
            p => p.Permission,
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAllowed(IAgentTool tool)
    {
        var key = ToolKey.Parse(tool.PermissionScope);
        var runtimeKey = RuntimeKey(tool.Name);
        var permission = PermissionFor(key.SkillName, key.ToolName)
            ?? PermissionFor(key.SkillName, string.Empty)
            ?? PermissionFor(runtimeKey.SkillName, runtimeKey.ToolName)
            ?? PermissionFor(runtimeKey.SkillName, string.Empty)
            ?? ToolPermission.Allow;

        return permission is ToolPermission.Allow;
    }

    private ToolPermission? PermissionFor(string skill, string tool)
        => _permissions.TryGetValue(Key(skill, tool), out var permission) ? permission : null;

    private static ToolKey RuntimeKey(string runtimeName)
    {
        if (runtimeName.StartsWith("browser__", StringComparison.Ordinal))
            return new ToolKey("browser", runtimeName);
        if (runtimeName.Contains("__", StringComparison.Ordinal))
        {
            var parts = runtimeName.Split("__", 2, StringSplitOptions.TrimEntries);
            return new ToolKey(parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        }

        return new ToolKey("builtin", NormalizeBuiltin(runtimeName));
    }

    public static ToolKey NormalizeDashboardKey(string key)
    {
        var parsed = ToolKey.Parse(key);
        if (parsed.SkillName is "builtin" or "builtins" or "agent_toolset")
            return new ToolKey("builtin", NormalizeBuiltin(parsed.ToolName));
        if (parsed.SkillName is "browser" or "internal_browser")
            return new ToolKey("browser", parsed.ToolName);
        return parsed;
    }

    private static string NormalizeBuiltin(string name) => name.Trim().ToLowerInvariant() switch
    {
        "bash" => "shell",
        "read" => "file_read",
        "write" => "file_write",
        "edit" => "file_edit",
        "grep" => "content_search",
        "glob" => "glob_search",
        "web_search" => "http_request",
        var other => other,
    };

    private static string Key(string skill, string tool) => $"{skill.Trim().ToLowerInvariant()}:{tool.Trim()}";
}

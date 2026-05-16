namespace OffceOs.Domain.Features.AgentHarness;

/// <summary>
/// Represents a "skill:tool" key. Keys without ":" are treated as
/// skill-level defaults with an empty tool name.
/// </summary>
public readonly record struct ToolKey
{
    public string SkillName { get; }
    public string ToolName { get; }

    public ToolKey(string skillName, string toolName)
    {
        SkillName = (skillName ?? string.Empty).Trim().ToLowerInvariant();
        ToolName = (toolName ?? string.Empty).Trim();
    }

    public static ToolKey Parse(string key)
    {
        var k = (key ?? string.Empty).Trim();
        var idx = k.IndexOf(':');
        if (idx <= 0) return new ToolKey(k, string.Empty);
        return new ToolKey(k[..idx], k[(idx + 1)..]);
    }

    public override string ToString() =>
        string.IsNullOrEmpty(ToolName) ? SkillName : $"{SkillName}:{ToolName}";
}

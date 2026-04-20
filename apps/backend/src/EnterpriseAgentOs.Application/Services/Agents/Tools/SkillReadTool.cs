using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents.Tools;

/// <summary>
/// Reads the full SKILL.md documentation for an installed skill.
/// Like Claude Code's skill system: only headers are in the prompt,
/// the agent reads the full body on demand.
/// </summary>
public sealed class SkillReadTool : IAgentTool
{
    private readonly IReadOnlyList<SkillRecord> _skills;

    public SkillReadTool(IReadOnlyList<SkillRecord> skills) => _skills = skills;

    public string Name => "skill_read";
    public ToolSchema Schema => new("skill_read",
        "Read the full documentation (SKILL.md) for an installed skill. " +
        "Use this to understand how a skill works before executing it.",
        new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string", description = "Skill name (e.g. 'notion', 'github')" }
            },
            required = new[] { "name" }
        });

    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
    {
        var name = args.GetProperty("name").GetString() ?? "";

        var skill = _skills.FirstOrDefault(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
        {
            var available = string.Join(", ", _skills.Select(s => s.Name));
            return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(false, "",
                $"Skill '{name}' not found. Available skills: {available}"));
        }

        if (string.IsNullOrEmpty(skill.Doc))
        {
            return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true,
                $"# {skill.Name}\n\n{skill.Description}\n\n(No additional documentation available.)"));
        }

        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, skill.Doc));
    }
}

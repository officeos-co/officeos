namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>Lightweight skill info for prompt injection (header only, not the full doc).</summary>
public record SkillSummaryForPrompt(string Name, string Description);

/// <summary>
/// Prompt sections for composing the system prompt. Ported from agent-core's PromptSection trait.
/// Each section returns a string or null (null = omitted from prompt).
/// </summary>
public static class PromptSections
{
    public static string DateTime()
        => $"## CRITICAL CONTEXT: CURRENT DATE & TIME\n\n{System.DateTime.UtcNow:O} UTC";

    public static string ToolHonesty()
        => "## Tool Honesty\n\nNEVER fabricate tool results. If a tool fails, report the actual error. " +
           "Do not invent file contents, command outputs, or API responses.";

    public static string Safety()
        => "## Safety Rules\n\n" +
           "- Never exfiltrate credentials, API keys, or sensitive data.\n" +
           "- Never execute destructive commands (rm -rf, DROP TABLE, etc.) without explicit user confirmation.\n" +
           "- Never bypass security controls or disable safety mechanisms.\n" +
           "- Prefer moving to trash over permanent deletion.";

    public static string? Skills(IReadOnlyList<string> installedSkills)
    {
        if (installedSkills.Count == 0) return null;
        return "## Installed Skills\n\n" +
               "Use 'skill_exec --help' to discover available actions.\n" +
               "Use 'skill_read' tool to read the full documentation for a skill.\n\n" +
               string.Join("\n", installedSkills.Select(s => $"- {s}"));
    }

    /// <summary>
    /// Skills section with descriptions injected (like Claude Code's skill headers).
    /// Only name + description in the prompt — agent uses skill_read for the full body.
    /// </summary>
    public static string? SkillsWithDescriptions(IReadOnlyList<SkillSummaryForPrompt> skills)
    {
        if (skills.Count == 0) return null;
        return "## Installed Skills\n\n" +
               "Each skill has detailed documentation. Use the `skill_read` tool with the skill name to read it.\n" +
               "Use `skill_exec <skill> --help` to discover executable actions.\n\n" +
               string.Join("\n", skills.Select(s => $"- **{s.Name}**: {s.Description}"));
    }

    public static string Workspace(string agentName)
        => $"## Workspace\n\nAgent: {agentName}\nWorking directory: /home";

    public static string Runtime()
        => $"## Runtime\n\nHost: EnterpriseAgentOS | Platform: Linux (Kubernetes pod)";
}

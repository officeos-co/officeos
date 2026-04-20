using EnterpriseAgentOs.Domain.Models;

namespace EnterpriseAgentOs.Application.Services.Agents;

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

    /// <summary>
    /// Skills section with full documentation injected (like Claude Code's skill headers).
    /// Each skill's Doc (SKILL.md) is included so the agent knows available actions upfront.
    /// </summary>
    public static string? SkillsWithDocs(IReadOnlyList<SkillRecord> skills)
    {
        if (skills.Count == 0) return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Installed Skills");
        sb.AppendLine();

        foreach (var skill in skills)
        {
            sb.AppendLine($"### {skill.Name}");
            sb.AppendLine();
            sb.AppendLine(skill.FormatPromptSection());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string Workspace(string agentName)
        => $"## Workspace\n\nAgent: {agentName}\nWorking directory: /home";

    public static string Runtime()
        => $"## Runtime\n\nHost: EnterpriseAgentOS | Platform: Linux (Kubernetes pod)";
}

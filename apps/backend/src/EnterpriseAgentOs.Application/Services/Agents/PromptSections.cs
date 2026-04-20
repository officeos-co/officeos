using EnterpriseAgentOs.Domain.Models;

namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// System prompt sections composed in OpenClaw order:
/// 1. Tooling  2. Safety  3. Skills  4. Workspace  5. Project Context (bootstrap files)
/// 6. Current Date &amp; Time  7. Runtime
/// Each section returns a string or null (null = omitted from prompt).
/// </summary>
public static class PromptSections
{
    // ── 1. Tooling ──────────────────────────────────────────────────────────

    public static string Tooling()
        => "## Tooling\n\n" +
           "NEVER fabricate tool results. If a tool fails, report the actual error. " +
           "Do not invent file contents, command outputs, or API responses.\n" +
           "Use `skill_exec <skill> <action>` to run skill actions. " +
           "Use `skill_read <skill>` to read full documentation for a skill.";

    // ── 2. Safety ───────────────────────────────────────────────────────────

    public static string Safety()
        => "## Safety\n\n" +
           "- Never exfiltrate credentials, API keys, or sensitive data.\n" +
           "- Never execute destructive commands (rm -rf, DROP TABLE, etc.) without explicit user confirmation.\n" +
           "- Never bypass security controls or disable safety mechanisms.\n" +
           "- Prefer moving to trash over permanent deletion.\n" +
           "- Internal actions (reading, organizing, learning) — do freely.\n" +
           "- External actions (sending emails, posting, API calls to third parties) — require explicit confirmation.";

    // ── 3. Skills ───────────────────────────────────────────────────────────

    /// <summary>
    /// Skills section with full documentation injected.
    /// Each skill's Doc (SKILL.md) is included so the agent knows available actions upfront.
    /// </summary>
    public static string? Skills(IReadOnlyList<SkillRecord> skills)
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

    // ── 4. Workspace ────────────────────────────────────────────────────────

    public static string Workspace(string agentName)
        => $"## Workspace\n\nAgent: {agentName}\nWorking directory: /home";

    // ── 5. Project Context (bootstrap files) ────────────────────────────────

    /// <summary>
    /// Composes bootstrap personality files (AGENTS.md, SOUL.md, IDENTITY.md, USER.md, etc.)
    /// into the "Project Context" section, matching OpenClaw's workspace files injection.
    /// </summary>
    public static string? ProjectContext(IReadOnlyList<AgentPersonalityRecord> files, string? userPrompt)
    {
        var ordered = files.OrderBy(f => f.CompositionOrder).ToList();
        if (ordered.Count == 0 && string.IsNullOrWhiteSpace(userPrompt)) return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Project Context");
        sb.AppendLine();

        foreach (var file in ordered)
        {
            sb.AppendLine($"<file path=\"{file.FileName}\">");
            sb.AppendLine(file.Content.Trim());
            sb.AppendLine("</file>");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            sb.AppendLine("<file path=\"PROMPT.md\">");
            sb.AppendLine(userPrompt.Trim());
            sb.AppendLine("</file>");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    // ── 6. Memory ───────────────────────────────────────────────────────────

    /// <summary>
    /// Composes agent memories into a section, matching OpenClaw's MEMORY.md approach.
    /// </summary>
    public static string? Memory(IReadOnlyList<AgentMemoryRecord> memories)
    {
        if (memories.Count == 0) return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Memory");
        sb.AppendLine();

        foreach (var m in memories)
        {
            sb.AppendLine($"### {m.Key}");
            sb.AppendLine(m.Content);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    // ── 7. Current Date & Time ──────────────────────────────────────────────

    public static string DateTime()
        => $"## Current Date & Time\n\n{System.DateTime.UtcNow:O} UTC";

    // ── 8. Runtime ──────────────────────────────────────────────────────────

    public static string Runtime()
        => "## Runtime\n\nHost: EnterpriseAgentOS | Platform: Linux (Kubernetes pod)";
}

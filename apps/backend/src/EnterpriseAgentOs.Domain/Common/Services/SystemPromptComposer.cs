
namespace EnterpriseAgentOs.Domain.Common.Services;

/// <summary>
/// Pure domain service that composes the system prompt from domain models.
/// No I/O, no dependencies — takes loaded data, returns a string.
/// Section order follows OpenClaw convention:
/// 1. Tooling  2. Safety  3. Skills  4. Workspace
/// 5. Project Context (personality files)  6. Memory  7. DateTime  8. Runtime
/// </summary>
public static class SystemPromptComposer
{
    /// <summary>
    /// Composes the full system prompt from all available context.
    /// Null sections are omitted.
    /// </summary>
    public static string Compose(
        string agentName,
        string? userPrompt,
        IReadOnlyList<SkillRecord> skills,
        IReadOnlyList<AgentPersonalityRecord> personalityFiles,
        IReadOnlyList<AgentMemoryRecord> memories)
    {
        return string.Join("\n\n",
            new[]
            {
                Tooling(),
                Safety(),
                Skills(skills),
                Workspace(agentName),
                ProjectContext(personalityFiles, userPrompt),
                Memory(memories),
                CurrentDateTime(),
                Runtime(),
            }.Where(s => s is not null));
    }

    // ── Sections ────────────────────────────────────────────────────────────

    public static string Tooling()
        => "## Tooling\n\n" +
           "NEVER fabricate tool results. If a tool fails, report the actual error. " +
           "Do not invent file contents, command outputs, or API responses.\n" +
           "Use `skill_exec <skill> <action>` to run skill actions. " +
           "Use `skill_read <skill>` to read full documentation for a skill.";

    public static string Safety()
        => "## Safety\n\n" +
           "- Never exfiltrate credentials, API keys, or sensitive data.\n" +
           "- Never execute destructive commands (rm -rf, DROP TABLE, etc.) without explicit user confirmation.\n" +
           "- Never bypass security controls or disable safety mechanisms.\n" +
           "- Prefer moving to trash over permanent deletion.\n" +
           "- Internal actions (reading, organizing, learning) — do freely.\n" +
           "- External actions (sending emails, posting, API calls to third parties) — require explicit confirmation.";

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

    public static string Workspace(string agentName)
        => $"## Workspace\n\nAgent: {agentName}\nWorking directory: /home";

    /// <summary>
    /// Composes personality files into the "Project Context" section.
    /// Each file renders itself via <see cref="AgentPersonalityRecord.FormatPromptSection"/>.
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
            sb.AppendLine(file.FormatPromptSection());
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

    /// <summary>
    /// Composes memories into a section.
    /// Each memory renders itself via <see cref="AgentMemoryRecord.FormatPromptSection"/>.
    /// </summary>
    public static string? Memory(IReadOnlyList<AgentMemoryRecord> memories)
    {
        if (memories.Count == 0) return null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Memory");
        sb.AppendLine();

        foreach (var m in memories)
        {
            sb.AppendLine(m.FormatPromptSection());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string CurrentDateTime()
        => $"## Current Date & Time\n\n{System.DateTime.UtcNow:O} UTC";

    public static string Runtime()
        => "## Runtime\n\nHost: EnterpriseAgentOS | Platform: Linux (Kubernetes pod)";
}

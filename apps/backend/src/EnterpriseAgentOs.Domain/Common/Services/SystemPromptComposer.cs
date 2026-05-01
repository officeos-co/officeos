
namespace EnterpriseAgentOs.Domain.Common.Services;

/// <summary>
/// Pure domain service that composes the system prompt from domain models.
/// No I/O, no dependencies — takes loaded data, returns a string.
/// Section order follows OpenClaw convention:
/// 1. Tooling  2. Safety  3. Workspace
/// 5. Project Context (personality files)  6. Memory  7. DateTime  8. Runtime
/// </summary>
public static class SystemPromptComposer
{
    /// <summary>
    /// Composes the full system prompt from all available context.
    /// Null sections are omitted.
    /// </summary>
    /// <summary>
    /// Composes the system prompt from the agent aggregate — uses the agent's
    /// personality files, memories, and skill details directly.
    /// </summary>
    public static string Compose(AgentRecord agent) =>
        Compose(agent.Name, agent.Prompt, agent.PersonalityFiles, agent.Memories);

    public static string Compose(
        string agentName,
        string? userPrompt,
        IReadOnlyList<AgentPersonalityRecord> personalityFiles,
        IReadOnlyList<AgentMemoryRecord> memories)
    {
        return string.Join("\n\n",
            new[]
            {
                Identity(agentName),
                Tooling(),
                Safety(),
                FileWork(),
                TaskWork(),
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
           "- NEVER fabricate tool results. If a tool fails, report the actual error.\n" +
           "- Do not invent file contents, command outputs, API responses, MCP results, or browser state.\n" +
           "- Prefer dedicated tools over shell commands: use file_read/file_edit/file_write for files, content_search/glob_search for search, and MCP resource tools for MCP resources.\n" +
           "- Use tool_search when you need to discover a less common built-in, MCP, or browser tool.";

    public static string Identity(string agentName)
        => $"## Identity\n\nYou are {agentName}, an EnterpriseAgentOS coding agent running in a Linux Kubernetes pod.";

    public static string Safety()
        => "## Safety\n\n" +
           "- Never exfiltrate credentials, API keys, or sensitive data.\n" +
           "- Never execute destructive commands (rm -rf, DROP TABLE, etc.) without explicit user confirmation.\n" +
           "- Never bypass security controls or disable safety mechanisms.\n" +
           "- Prefer moving to trash over permanent deletion.\n" +
           "- Execute tasks directly — do not ask for confirmation before using installed skills or tools.";

    public static string FileWork()
        => "## File Work\n\n" +
           "- Read files before editing or overwriting them.\n" +
           "- Prefer targeted edits over full rewrites.\n" +
           "- Preserve user changes and unrelated work.\n" +
           "- Run the narrowest meaningful tests/checks after code changes.\n" +
           "- Only commit or push when the user explicitly asks.";

    public static string TaskWork()
        => "## Task Tracking\n\n" +
           "- For multi-step work, create tasks and keep progress current.\n" +
           "- Keep one task in progress at a time unless work is truly parallel.\n" +
           "- Mark tasks completed only when the work and verification are done.\n" +
           "- If blocked by a user preference, use ask_user_question with concrete options.";

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

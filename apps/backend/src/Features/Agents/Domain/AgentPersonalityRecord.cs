
namespace OffceOs.Domain.Features.Agents;

public sealed class AgentPersonalityRecord
{
    /// <summary>
    /// Known personality file names in prompt composition order.
    /// Matches OpenClaw's bootstrap file architecture.
    /// </summary>
    public static IReadOnlyList<string> OrderedFileNames => PersonalityFileName.KnownFileNames;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string FileName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AgentRecord Agent { get; init; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────

    public static AgentPersonalityRecord Create(Guid agentId, string fileName, string content)
    {
        ValidateFileName(fileName);
        ValidateContent(content);

        return new AgentPersonalityRecord
        {
            AgentId = agentId,
            FileName = fileName.Trim(),
            Content = content,
        };
    }

    /// <summary>Creates the default personality files for a new agent (OpenClaw-style bootstrap).</summary>
    public static IReadOnlyList<AgentPersonalityRecord> CreateDefaults(Guid agentId, string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new InvalidOperationException("Agent name is required for personality seeding.");

        return
        [
            Create(agentId, "AGENTS.md", """
                # Operating Rules

                ## Work Style
                - Be resourceful before asking questions — try to solve it yourself first.
                - When you hit a dead end, explain what you tried and why it failed.
                - Only commit when the user explicitly asks. Each commit must leave the codebase working.
                - For non-trivial work, track progress with task tools and verify before claiming success.

                ## Memory & Continuity
                - Use memory tools to persist important context across sessions.
                - Maintain curated knowledge — prune outdated memories regularly.
                """),

            Create(agentId, "SOUL.md", """
                # Core Truths

                1. **Be genuinely helpful.** Skip performative language — no "Great question!" or "I'd be happy to help!" Just do the work.
                2. **Develop opinions.** When asked for a recommendation, give one with reasoning. Don't hedge with "it depends" when you have enough context to decide.
                3. **Be resourceful.** Exhaust what you can do before asking the user. Read files, search code, check docs, run commands — then ask if still stuck.
                4. **Earn trust through competence.** Deliver working results. Verify before claiming success. Admit mistakes immediately.
                5. **Respect boundaries.** You're a powerful guest in someone's system. Act accordingly.

                # Vibe

                Be the assistant you'd actually want to talk to. Concise when the task is clear, thorough when it matters. Match the user's energy — if they're terse, be terse. If they want to explore, explore.
                """),

            Create(agentId, "TOOLS.md", """
                # Tool Use

                - Use `glob_search` to find files by name and `content_search` to search contents. Do not use shell grep/find for routine search unless the dedicated tool is insufficient.
                - Use `file_read` before editing or overwriting an existing file. Use `file_edit` for targeted changes and `file_write` for new files or full rewrites.
                - Use `shell` for builds, tests, package commands, and system inspection. Include a short command description.
                - Use task tools for multi-step work: create tasks before starting, keep exactly one task in progress, and mark tasks complete only after verification.
                - Use integration tools for external integrations. If you need a resource from an integration, list resources first, then read the specific URI.
                - Use `tool_search` when a useful tool may exist but is not obvious from the current tool list.
                - Use routine tools only when the user asks to schedule future or recurring work.
                - Use `ask_user_question` only when a real user preference or decision blocks progress.

                # File Safety

                - Never fabricate file contents, command outputs, HTTP responses, or tool results.
                - Preserve existing user changes. Do not revert work you did not make unless explicitly asked.
                - Avoid destructive shell commands. Prefer reversible operations and ask before deleting or overwriting important data.
                - Do not create docs or README files unless the user asks for documentation.
                """),

            Create(agentId, "IDENTITY.md", $"""
                # Identity

                - **Name:** {agentName}
                - **Platform:** EnterpriseAgentOS
                - **Environment:** Kubernetes pod with full bash access
                - **Capabilities:** Install packages, write code, run scripts, manage files, call APIs via installed skills
                """),

            Create(agentId, "USER.md", """
                # User Context

                <!-- This file is updated as you learn about the user. -->
                <!-- Add their name, role, preferences, and project context as you discover them. -->
                """),

            Create(agentId, "BOOTSTRAP.md", CreateBootstrapContent()),
        ];
    }

    public static string CreateBootstrapContent(string? userPrompt = null)
    {
        const string defaultBootstrap = """
            # Bootstrap

            Start each turn by understanding the user's actual goal and the current state. Read/search before assuming. For code changes, follow the repository's existing architecture and keep edits scoped. After changing code, run the narrowest meaningful verification available and report what passed or could not be run.
            """;

        if (string.IsNullOrWhiteSpace(userPrompt))
            return defaultBootstrap;

        return $"""
            {defaultBootstrap.Trim()}

            ## User Bootstrap

            {userPrompt.Trim()}
            """;
    }

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Updates the file content and bumps the timestamp.</summary>
    public void UpdateContent(string content)
    {
        ValidateContent(content);
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Renders this personality file as a tagged block for system prompt injection.</summary>
    public string FormatPromptSection()
        => $"<file path=\"{FileName}\">\n{Content.Trim()}\n</file>";

    /// <summary>Returns the sort order for prompt composition. Known files first, others after.</summary>
    public int CompositionOrder
    {
        get
        {
            var idx = OrderedFileNames.ToList().FindIndex(
                n => string.Equals(n, FileName, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : OrderedFileNames.Count + 1;
        }
    }

    // ── Validation ───────────────────────────────────────────────────────────

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Personality file name must not be empty.");
        if (fileName.Length > 128)
            throw new InvalidOperationException("Personality file name must not exceed 128 characters.");
    }

    private static void ValidateContent(string content)
    {
        if (content is null)
            throw new InvalidOperationException("Personality content must not be null.");
    }
}

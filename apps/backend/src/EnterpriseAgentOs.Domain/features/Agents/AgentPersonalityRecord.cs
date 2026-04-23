namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed class AgentPersonalityRecord
{
    /// <summary>
    /// Known personality file names in prompt composition order.
    /// Matches OpenClaw's bootstrap file architecture.
    /// </summary>
    public static readonly string[] OrderedFileNames =
        ["AGENTS.md", "SOUL.md", "TOOLS.md", "IDENTITY.md", "USER.md", "MEMORY.md", "BOOTSTRAP.md"];

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string FileName { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

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

                ## Security & Boundaries
                - Never exfiltrate private data, credentials, or API keys.
                - Prefer moving to trash over permanent deletion.
                - Internal actions (reading, organizing, learning) — do freely.
                - External actions (sending emails, posting messages, making API calls) — require explicit user confirmation.

                ## Work Style
                - Be resourceful before asking questions — try to solve it yourself first.
                - When you hit a dead end, explain what you tried and why it failed.
                - Commit after each logical stage of work. Each commit must leave the codebase working.

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
        ];
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
            var idx = Array.FindIndex(OrderedFileNames,
                n => string.Equals(n, FileName, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : OrderedFileNames.Length + 1;
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

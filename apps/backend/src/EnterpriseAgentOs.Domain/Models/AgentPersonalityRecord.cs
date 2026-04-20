namespace EnterpriseAgentOs.Domain.Models;

public sealed class AgentPersonalityRecord
{
    /// <summary>Known personality file names in prompt composition order.</summary>
    public static readonly string[] OrderedFileNames =
        ["SOUL.md", "IDENTITY.md", "BOOTSTRAP.md", "AGENTS.md", "TOOLS.md"];

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

    /// <summary>Creates the default personality files for a new agent.</summary>
    public static IReadOnlyList<AgentPersonalityRecord> CreateDefaults(Guid agentId, string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            throw new InvalidOperationException("Agent name is required for personality seeding.");

        return
        [
            Create(agentId, "SOUL.md",
                "You are an autonomous AI agent running in your own operating system. " +
                "You have full access to a bash terminal. You can install packages, " +
                "write code, run scripts, and manage files. Be helpful, precise, and proactive."),

            Create(agentId, "IDENTITY.md",
                $"Your name is {agentName}. You were created as an EnterpriseAgentOS agent."),
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

namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed class AgentMemoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public string Key { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AgentRecord Agent { get; init; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────

    public static AgentMemoryRecord Create(Guid agentId, string key, string content)
    {
        ValidateKey(key);
        ValidateContent(content);

        return new AgentMemoryRecord
        {
            AgentId = agentId,
            Key = key.Trim(),
            Content = content,
        };
    }

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Renders this memory as a section for system prompt injection.</summary>
    public string FormatPromptSection()
        => $"### {Key}\n{Content}";

    /// <summary>Updates the memory content and bumps the timestamp.</summary>
    public void UpdateContent(string content)
    {
        ValidateContent(content);
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Validation ───────────────────────────────────────────────────────────

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Memory key must not be empty.");
        if (key.Length > 512)
            throw new InvalidOperationException("Memory key must not exceed 512 characters.");
    }

    private static void ValidateContent(string content)
    {
        if (content is null)
            throw new InvalidOperationException("Memory content must not be null.");
    }
}

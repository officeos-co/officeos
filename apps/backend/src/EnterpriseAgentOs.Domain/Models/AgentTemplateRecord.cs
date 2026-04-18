namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// Pre-configured agent template surfaced in the Quickstart wizard.
/// Built-in templates are seeded at startup; operators may create org-scoped
/// templates via the dashboard (OwnerId non-null).
/// </summary>
public sealed class AgentTemplateRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    /// <summary>System prompt the agent boots with.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>JSON array of skill names (e.g. ["github","notion"]).</summary>
    public string IntegrationsJson { get; set; } = "[]";

    /// <summary>JSON array of channel slugs (e.g. ["slack","discord"]).</summary>
    public string ChannelsJson { get; set; } = "[]";

    public bool IsBuiltin { get; set; }

    /// <summary>Null for built-in templates.</summary>
    public Guid? OwnerId { get; set; }
    public UserRecord? Owner { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

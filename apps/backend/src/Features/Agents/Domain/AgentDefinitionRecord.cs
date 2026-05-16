namespace OffceOs.Features.Agents.Domain;

public sealed class AgentDefinitionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public int Version { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required, MaxLength(64)]
    public string Provider { get; init; } = string.Empty;

    [MaxLength(128)]
    public string? Model { get; init; }

    public string? SystemPrompt { get; init; }

    [Required]
    public string ConfigJson { get; init; } = "{}";

    [Required, MaxLength(128)]
    public string ConfigHash { get; init; } = string.Empty;

    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed record AgentDefinitionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public bool ActiveOnly { get; init; }
}

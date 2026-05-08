namespace EnterpriseAgentOs.Domain.Features.Agents;

public static class AgentResourceTypes
{
    public const string Browser = "browser";
    public const string MemoryStore = "memory_store";
    public const string Channel = "channel";
}

public static class AgentResourceAccessModes
{
    public const string ReadWrite = "read_write";
    public const string ReadOnly = "read_only";
}

public sealed class BrowserResourceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? CurrentAgentId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static BrowserResourceRecord Create(Guid ownerId, string displayName) => new()
    {
        OwnerId = ownerId,
        DisplayName = NormalizeName(displayName, "Browser"),
    };

    internal static string NormalizeName(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

public sealed class AgentSessionResourceAttachmentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }
    public Guid SessionId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public Guid ResourceId { get; init; }
    public string AccessMode { get; set; } = AgentResourceAccessModes.ReadWrite;
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

namespace OffceOs.Domain.Features.Agents;

public static class AgentResourceKinds
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

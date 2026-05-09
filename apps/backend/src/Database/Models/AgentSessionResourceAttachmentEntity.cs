namespace OffceOs.Database.Models;

public sealed class AgentSessionResourceAttachmentEntity
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid SessionId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string AccessMode { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentEntity? Agent { get; set; }
    public AgentSessionEntity? Session { get; set; }
}

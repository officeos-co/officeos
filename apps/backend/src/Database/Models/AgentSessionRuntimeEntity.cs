namespace OffceOs.Database.Models;

public sealed class AgentSessionRuntimeEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string SandboxId { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AgentSessionEntity? Session { get; set; }
}

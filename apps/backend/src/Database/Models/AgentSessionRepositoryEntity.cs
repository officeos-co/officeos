namespace OffceOs.Database.Models;

public sealed class AgentSessionRepositoryEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
    public string? BaseBranch { get; set; }
    public string? CredentialRef { get; set; }
    public string? Branch { get; set; }
    public DateTime CreatedAt { get; set; }
    public AgentSessionEntity? Session { get; set; }
}

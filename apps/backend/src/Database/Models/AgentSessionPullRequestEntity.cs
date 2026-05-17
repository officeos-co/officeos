namespace OffceOs.Database.Models;

public sealed class AgentSessionPullRequestEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int? Number { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AgentSessionEntity? Session { get; set; }
}

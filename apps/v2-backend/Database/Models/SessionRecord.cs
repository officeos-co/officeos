namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class SessionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [Required, MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public UserRecord? User { get; set; }
}

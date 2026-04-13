namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class UserRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(256)]
    public string? Name { get; set; }
    [MaxLength(1024)]
    public string? AvatarUrl { get; set; }
    [Required, MaxLength(256)]
    public string GoogleSubjectId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}

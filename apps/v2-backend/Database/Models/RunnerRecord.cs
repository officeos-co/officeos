namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class RunnerRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(32)]
    public string Status { get; set; } = "pending";
    [Required, MaxLength(128)]
    public string RegistrationTokenHash { get; set; } = string.Empty;
    [MaxLength(128)]
    public string? AuthTokenHash { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    [MaxLength(64)]
    public string? Version { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public UserRecord? Owner { get; set; }
}

namespace EnterpriseAgentOs.Domain.Features.Auth;

public sealed class DeviceCodeRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string UserCode { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    [Required, MaxLength(16)]
    public string Status { get; set; } = "pending";

    [MaxLength(200)]
    public string? RunnerName { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastPolledAt { get; set; }

    public UserRecord? User { get; set; }
}

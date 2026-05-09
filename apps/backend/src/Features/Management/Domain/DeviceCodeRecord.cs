namespace OffceOs.Domain.Features.Management;

public sealed class DeviceCodeRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string DeviceCode { get; init; } = string.Empty;

    [Required, MaxLength(16)]
    public string UserCode { get; init; } = string.Empty;

    public Guid? UserId { get; set; }

    public DeviceCodeStatus Status { get; set; } = DeviceCodeStatus.Pending;

    [MaxLength(200)]
    public string? RunnerName { get; set; }

    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastPolledAt { get; set; }

    public UserRecord? User { get; set; }
}

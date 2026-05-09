namespace OffceOs.Database.Models;

public sealed class DeviceCodeEntity
{
    public Guid Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string Status { get; set; } = "pending";
    public string? RunnerName { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPolledAt { get; set; }
    public UserEntity? User { get; set; }
}

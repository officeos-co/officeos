namespace OffceOs.Domain.Features.Management;

public sealed class SessionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    [Required, MaxLength(128)]
    public string TokenHash { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public UserRecord? User { get; init; }
}

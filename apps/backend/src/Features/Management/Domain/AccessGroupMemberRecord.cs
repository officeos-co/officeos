namespace OffceOs.Domain.Features.Management;

public sealed class AccessGroupMemberRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AccessGroupId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

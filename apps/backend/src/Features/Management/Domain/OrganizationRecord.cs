namespace OffceOs.Domain.Features.Management;

public sealed class OrganizationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

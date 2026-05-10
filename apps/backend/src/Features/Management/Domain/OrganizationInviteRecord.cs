namespace OffceOs.Domain.Features.Management;

public sealed class OrganizationInviteRecord
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public OrgRole Role { get; init; } = OrgRole.Editor;
    public DateTime CreatedAt { get; init; }
}

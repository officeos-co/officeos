namespace OffceOs.Features.Management.Domain;

public sealed record WorkspaceMemberFilter
{
    public Guid? Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? UserId { get; init; }
}

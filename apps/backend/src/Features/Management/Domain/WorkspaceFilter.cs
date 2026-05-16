
namespace OffceOs.Features.Management.Domain;

public sealed record WorkspaceFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OwnerUserId { get; init; }
    public WorkspaceOwnerKind? OwnerKind { get; init; }
    public bool? IsDefault { get; init; }
}

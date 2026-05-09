namespace OffceOs.Domain.Features.Management;

public sealed record WorkspaceFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
}

namespace OffceOs.Domain.Features.Browser;

public sealed record BrowserResourceFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
}

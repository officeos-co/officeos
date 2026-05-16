namespace OffceOs.Features.Browser.Domain;

public sealed record BrowserResourceFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
}

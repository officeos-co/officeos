namespace OffceOs.Features.Channels.Domain;

public sealed record ChannelConnectionFilter
{
    public Guid? Id { get; init; }
    public string? ChannelType { get; init; }
    public Guid? CreatedById { get; init; }
    public Guid? WorkspaceId { get; init; }
}

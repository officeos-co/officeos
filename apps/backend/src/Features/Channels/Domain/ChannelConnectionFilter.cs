namespace EnterpriseAgentOs.Domain.Features.Channels;

public sealed record ChannelConnectionFilter
{
    public Guid? Id { get; init; }
    public string? ChannelType { get; init; }
    public Guid? CreatedById { get; init; }
}

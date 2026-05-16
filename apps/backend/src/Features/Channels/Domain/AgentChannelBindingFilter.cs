namespace OffceOs.Features.Channels.Domain;

public sealed record AgentChannelBindingFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? ChannelConnectionId { get; init; }
}

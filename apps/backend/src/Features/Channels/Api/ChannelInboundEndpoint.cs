using EnterpriseAgentOs.Domain.Features.Agents;

namespace EnterpriseAgentOs.Api.Features.Channels;

public static class ChannelInboundEndpoint
{
    public record ChannelInboundRequest(
        string ChannelType,
        string SenderIdentifier,
        string MessageText,
        bool IsGroupMessage,
        string? MessageId,
        string? ChannelId);

    public static async Task<IResult> Handle(
        ChannelInboundRequest request,
        IChannelService channelService,
        CancellationToken ct)
    {
        var agentIds = await channelService.RouteInboundByChannelTypeAsync(
            request.ChannelType, request.SenderIdentifier, request.MessageText,
            request.IsGroupMessage, request.MessageId, request.ChannelId, ct);

        return Results.Ok(new { agentIds });
    }
}

namespace OffceOs.Api.Features.Channels;

public static class ChannelInboundEndpoint
{
    public record ChannelInboundInput(
        string ChannelType,
        string SenderIdentifier,
        string MessageText,
        bool IsGroupMessage,
        string? MessageId,
        string? ChannelId);

    public static async Task<IResult> Handle(
        ChannelInboundInput request,
        IChannelService channelService,
        CancellationToken ct)
    {
        var agentIds = await channelService.RouteInboundByChannelTypeAsync(
            request.ChannelType, request.SenderIdentifier, request.MessageText,
            request.IsGroupMessage, request.MessageId, request.ChannelId, ct);

        return Results.Ok(new { agentIds });
    }
}

using EnterpriseAgentOs.Domain.Events;
using EnterpriseAgentOs.Domain.Features.AgentLogs;
using EnterpriseAgentOs.Domain.Features.Channels;
using MediatR;

namespace EnterpriseAgentOs.Api.Features.Channels;

public static class ChannelInboundEndpoint
{
    public record ChannelInboundRequest(
        Guid ConnectionId,
        string SenderIdentifier,
        string MessageText,
        bool IsGroupMessage,
        string? MessageId,
        string? ChannelId);

    public static async Task<IResult> Handle(
        ChannelInboundRequest request,
        IChannelRepository channelRepository,
        IPublisher publisher,
        CancellationToken ct)
    {
        var bindings = await channelRepository.FindBindingsByConnectionAsync(request.ConnectionId, ct);
        var agentIds = new List<Guid>();

        foreach (var binding in bindings)
        {
            if (!binding.Enabled) continue;

            var channelType = binding.ChannelConnection?.ChannelType ?? "unknown";
            var correlationId = Guid.NewGuid().ToString("N");

            await publisher.Publish(new ChannelMessageRoutedEvent(
                binding.AgentId, AgentLogType.ChannelIn, channelType,
                request.MessageText, correlationId), ct);

            await publisher.Publish(new MessageReceivedEvent(
                binding.AgentId, request.MessageText, correlationId, null), ct);

            agentIds.Add(binding.AgentId);
        }

        return Results.Ok(new { agentIds });
    }
}

using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Features.Channels;
using OffceOs.Application.Features;
using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
namespace OffceOs.EventHandlers.Features.Channels;

internal sealed class BroadcastToChannelsHandler : INotificationHandler<MessageOutEvent>
{
    private readonly ChannelReplyContext _channelReplyContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BroadcastToChannelsHandler(
        ChannelReplyContext replyContext,
        IServiceScopeFactory scopeFactory)
    {
        _channelReplyContext = replyContext;
        _serviceScopeFactory = scopeFactory;
    }

    public Task Handle(MessageOutEvent notification, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(notification.Content))
            return Task.CompletedTask;

        var internalReply = _channelReplyContext.TakeInternal(notification.CorrelationId);
        if (internalReply is { } internalTarget)
        {
            if (internalTarget.ReplyingAgentId != notification.AgentId)
                return Task.CompletedTask;

            var replyingAgentId = notification.AgentId;
            var internalCorrelationId = notification.CorrelationId;
            var internalContent = notification.Content;

            BackgroundWork.Run<IPublisher, IResourceLogWriterService>(
                _serviceScopeFactory,
                async (publisher, resourceLogWriterService) =>
                {
                    await resourceLogWriterService
                        .ForChannel(internalTarget.ChannelConnectionId)
                        .WithAgent(replyingAgentId)
                        .WithCorrelation(internalCorrelationId)
                        .ChannelOutAsync(ChannelType.Internal.ToStorageString(), internalContent);

                    await resourceLogWriterService
                        .ForChannel(internalTarget.ChannelConnectionId)
                        .WithAgent(internalTarget.SourceAgentId)
                        .WithCorrelation(internalCorrelationId)
                        .ChannelInAsync(ChannelType.Internal.ToStorageString(), internalContent);

                    await publisher.Publish(new MessageReceivedEvent(
                        internalTarget.SourceAgentId,
                        internalContent,
                        internalCorrelationId,
                        AgentWorkPurposeKinds.Channel));
                });

            return Task.CompletedTask;
        }

        // Check if this turn was triggered by a channel message
        var reply = _channelReplyContext.Take(notification.CorrelationId);
        if (reply is null)
            return Task.CompletedTask;

        var (channelType, platformId, threadId, channelConnectionId) = reply.Value;
        var agentId = notification.AgentId;
        var correlationId = notification.CorrelationId;
        var content = notification.Content;

        BackgroundWork.Run<IChannelGateway, IResourceLogWriterService>(
            _serviceScopeFactory,
            async (gateway, resourceLogWriterService) =>
            {
                try
                {
                    await gateway.SendAsync(channelConnectionId, channelType, platformId, threadId,
                        ChannelMessage.Text(content), CancellationToken.None);

                    await resourceLogWriterService
                        .ForChannel(channelConnectionId)
                        .WithAgent(agentId)
                        .WithCorrelation(correlationId)
                        .ChannelOutAsync(channelType, content);
                }
                catch (Exception ex)
                {
                    await resourceLogWriterService
                        .ForChannel(channelConnectionId)
                        .WithAgent(agentId)
                        .WithCorrelation(correlationId)
                        .ErrorAsync(ex, "Failed to deliver reply via {ChannelType}", channelType);
                }
            });

        return Task.CompletedTask;
    }
}

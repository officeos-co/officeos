using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Channels;
using OffceOs.Database.Models;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Domain.Features.Channels;
using OffceOs.EventHandlers.Features.Channels;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Channels;

public sealed class InternalChannelTests
{
    [Fact]
    public async Task Internal_channel_routes_from_sender_to_receivers_and_blocks_reply_only_initiation()
    {
        await using var db = TestDbFactory.Create("internal-channel-routing");
        var publisher = new RecordingPublisher();
        var replyContext = new ChannelReplyContext();
        var service = CreateService(db, publisher, replyContext);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var sourceAgentId = Guid.NewGuid();
        var targetAgentId = Guid.NewGuid();
        SeedAgents(db, ownerId, workspaceId, sourceAgentId, targetAgentId);

        var channel = await service.CreateOwnedInternalConnectionAsync(
            "Planner to reviewer",
            [
                new InternalChannelBindingRequest(sourceAgentId, true, true, false, "planner"),
                new InternalChannelBindingRequest(targetAgentId, false, true, true, "reviewer"),
            ],
            ownerId,
            workspaceId);

        var tool = new InternalChannelSendTool(service, sourceAgentId);
        using var args = JsonDocument.Parse($$"""
        {
          "channel_connection_id": "{{channel.Id}}",
          "message": "review this plan"
        }
        """);
        var result = await tool.ExecuteAsync(args.RootElement);

        Assert.True(result.Value.Success);
        Assert.Contains(targetAgentId.ToString(), result.Value.Output);
        var message = Assert.Single(publisher.Notifications.OfType<MessageReceivedEvent>());
        Assert.Equal(targetAgentId, message.AgentId);
        Assert.Equal("review this plan", message.Content);
        Assert.Contains(publisher.Notifications.OfType<ChannelMessageRoutedEvent>(),
            routed => routed.AgentId == sourceAgentId && routed.LogType == AgentLogType.ChannelOut);
        Assert.Contains(publisher.Notifications.OfType<ChannelMessageRoutedEvent>(),
            routed => routed.AgentId == targetAgentId && routed.LogType == AgentLogType.ChannelIn);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendInternalMessageAsync(targetAgentId, channel.Id, "starting a new thread"));
    }

    [Fact]
    public async Task Internal_channel_reply_context_routes_assistant_output_back_to_source_agent()
    {
        await using var db = TestDbFactory.Create("internal-channel-reply");
        var publisher = new RecordingPublisher();
        var replyContext = new ChannelReplyContext();
        var service = CreateService(db, publisher, replyContext);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var sourceAgentId = Guid.NewGuid();
        var targetAgentId = Guid.NewGuid();
        SeedAgents(db, ownerId, workspaceId, sourceAgentId, targetAgentId);

        var channel = await service.CreateOwnedInternalConnectionAsync(
            "Planner to reviewer",
            [
                new InternalChannelBindingRequest(sourceAgentId, true, true, false, "planner"),
                new InternalChannelBindingRequest(targetAgentId, false, true, true, "reviewer"),
            ],
            ownerId,
            workspaceId);
        await service.SendInternalMessageAsync(sourceAgentId, channel.Id, "review this plan");
        var inbound = Assert.Single(publisher.Notifications.OfType<MessageReceivedEvent>());

        var services = new ServiceCollection()
            .AddSingleton<IPublisher>(publisher)
            .BuildServiceProvider();
        var handler = new BroadcastToChannelsHandler(
            replyContext,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BroadcastToChannelsHandler>.Instance);

        await handler.Handle(new MessageOutEvent(targetAgentId, inbound.CorrelationId, "looks good"), CancellationToken.None);

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline &&
               publisher.Notifications.OfType<MessageReceivedEvent>().Count() < 2)
        {
            await Task.Delay(10);
        }

        var reply = publisher.Notifications.OfType<MessageReceivedEvent>().Last();
        Assert.Equal(sourceAgentId, reply.AgentId);
        Assert.Equal("looks good", reply.Content);
        Assert.Equal(inbound.CorrelationId, reply.CorrelationId);
    }

    private static ChannelService CreateService(
        OffceOs.Database.EaosDbContext db,
        RecordingPublisher publisher,
        ChannelReplyContext replyContext)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-channel-test-keys-{Guid.NewGuid():N}");
        return new ChannelService(
            new ChannelRepository(db),
            new RecordingChannelGateway(),
            new AgentRepository(db),
            new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath))),
            publisher,
            replyContext,
            NullLogger<ChannelService>.Instance);
    }

    private static void SeedAgents(OffceOs.Database.EaosDbContext db, Guid ownerId, Guid workspaceId, params Guid[] agentIds)
    {
        db.Agents.AddRange(agentIds.Select((agentId, index) => new AgentEntity
        {
            Id = agentId,
            Name = $"Agent {index}",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = "running",
            CreatedAt = DateTime.UtcNow,
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
        }));
        db.SaveChanges();
    }
}

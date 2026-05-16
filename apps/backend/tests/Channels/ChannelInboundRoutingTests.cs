using OffceOs.Features.Channels.Application;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Database.Models;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Features.Channels.Domain;
using OffceOs.Common.Infrastructure.Security;
using OffceOs.Features.Agents.Infrastructure;
using OffceOs.Features.Channels.Infrastructure;
using OffceOs.Features.ResourceLogs.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Channels;

public sealed class ChannelInboundRoutingTests
{
    [Fact]
    public async Task RouteInbound_notifies_only_agents_bound_to_the_exact_connection()
    {
        await using var db = TestDbFactory.Create("channel-routing");
        var publisher = new RecordingPublisher();
        var service = CreateService(db, publisher);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var opsAgentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var supportAgentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        SeedAgents(db, ownerId, workspaceId, opsAgentIds.Concat(supportAgentIds).ToArray());

        var telegramOps = await service.CreateConnectionAsync("telegram", "Telegram Ops", null, ownerId, workspaceId);
        var telegramSupport = await service.CreateConnectionAsync("telegram", "Telegram Support", null, ownerId, workspaceId);
        foreach (var agentId in opsAgentIds)
            await service.BindAgentAsync(agentId, telegramOps.Id, null);
        foreach (var agentId in supportAgentIds)
            await service.BindAgentAsync(agentId, telegramSupport.Id, null);

        var notified = await service.RouteInboundAsync(
            telegramOps.Id,
            "sender-1",
            """{"text":"ops incident"}""",
            false,
            "message-1",
            "chat-ops");

        var messageEvents = publisher.Notifications.OfType<MessageReceivedEvent>().ToList();
        var logEvents = db.ResourceLogs.ToList();
        Assert.Equal(opsAgentIds.OrderBy(id => id), notified.OrderBy(id => id));
        Assert.Equal(opsAgentIds.OrderBy(id => id), messageEvents.Select(ev => ev.AgentId).OrderBy(id => id));
        Assert.DoesNotContain(messageEvents, ev => supportAgentIds.Contains(ev.AgentId));
        Assert.All(logEvents, log => Assert.Equal(telegramOps.Id, log.ChannelConnectionId));
        Assert.All(messageEvents, ev => Assert.Equal("ops incident", ev.Content));
    }

    [Fact]
    public async Task RouteInbound_logs_disabled_bindings_but_does_not_notify_them()
    {
        await using var db = TestDbFactory.Create("channel-disabled-binding");
        var publisher = new RecordingPublisher();
        var service = CreateService(db, publisher);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var enabledAgentId = Guid.NewGuid();
        var disabledAgentId = Guid.NewGuid();
        SeedAgents(db, ownerId, workspaceId, enabledAgentId, disabledAgentId);

        var telegram = await service.CreateConnectionAsync("telegram", "Telegram Ops", null, ownerId, workspaceId);
        await service.BindAgentAsync(enabledAgentId, telegram.Id, null);
        var disabledBinding = await service.BindAgentAsync(disabledAgentId, telegram.Id, null);
        await new ChannelRepository(db).UpdateBindingAsync(disabledBinding.Id, binding => binding.Enabled = false);

        var notified = await service.RouteInboundAsync(telegram.Id, "sender", "hello", false, "message", "chat");

        var messageEvents = publisher.Notifications.OfType<MessageReceivedEvent>().ToList();
        var logEvents = db.ResourceLogs.ToList();
        Assert.Equal([enabledAgentId], notified);
        Assert.Equal([enabledAgentId], messageEvents.Select(ev => ev.AgentId));
        Assert.Contains(logEvents, log => log.AgentId == enabledAgentId && log.ChannelConnectionId == telegram.Id);
        Assert.Contains(logEvents, log => log.AgentId == disabledAgentId && log.ChannelConnectionId == telegram.Id);
    }

    [Fact]
    public async Task RouteInbound_ignores_disabled_or_missing_connections()
    {
        await using var db = TestDbFactory.Create("channel-disabled-connection");
        var publisher = new RecordingPublisher();
        var service = CreateService(db, publisher);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        SeedAgents(db, ownerId, workspaceId, agentId);
        var telegram = await service.CreateConnectionAsync("telegram", "Telegram Ops", null, ownerId, workspaceId);
        await service.BindAgentAsync(agentId, telegram.Id, null);
        await service.UpdateConnectionAsync(telegram.Id, null, null, false);

        var disabledResult = await service.RouteInboundAsync(telegram.Id, "sender", "disabled", false, "message", "chat");
        var missingResult = await service.RouteInboundAsync(Guid.NewGuid(), "sender", "missing", false, "message", "chat");

        Assert.Empty(disabledResult);
        Assert.Empty(missingResult);
        Assert.Empty(publisher.Notifications.OfType<MessageReceivedEvent>());
        Assert.Empty(db.ResourceLogs);
    }

    [Fact]
    public async Task RouteInbound_scales_to_many_same_kind_connections_without_fanout()
    {
        await using var db = TestDbFactory.Create("channel-routing-extreme");
        var publisher = new RecordingPublisher();
        var service = CreateService(db, publisher);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var targetAgentIds = Array.Empty<Guid>();
        ChannelConnectionRecord? targetConnection = null;

        for (var connectionIndex = 0; connectionIndex < 50; connectionIndex++)
        {
            var connection = await service.CreateConnectionAsync("telegram", $"Telegram {connectionIndex}", null, ownerId, workspaceId);
            var agentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            SeedAgents(db, ownerId, workspaceId, agentIds);
            foreach (var agentId in agentIds)
                await service.BindAgentAsync(agentId, connection.Id, null);

            if (connectionIndex == 37)
            {
                targetConnection = connection;
                targetAgentIds = agentIds;
            }
        }

        var notified = await service.RouteInboundAsync(
            targetConnection!.Id,
            "sender",
            "target only",
            false,
            "message",
            "chat");

        var messageEvents = publisher.Notifications.OfType<MessageReceivedEvent>().ToList();
        Assert.Equal(targetAgentIds.OrderBy(id => id), notified.OrderBy(id => id));
        Assert.Equal(2, messageEvents.Count);
        Assert.Equal(targetAgentIds.OrderBy(id => id), messageEvents.Select(ev => ev.AgentId).OrderBy(id => id));
    }

    private static ChannelService CreateService(OffceOs.Database.EaosDbContext db, RecordingPublisher publisher)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-channel-test-keys-{Guid.NewGuid():N}");
        return new ChannelService(
            new ChannelRepository(db),
            new RecordingChannelGateway(),
            new AgentRepository(db),
            new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath))),
            publisher,
            new ChannelReplyContext(),
            new FakeResourceLogWriterService(
                new ResourceLogService(new ResourceLogRepository(db), new FakeControlPlaneResourceCatalogService())));
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

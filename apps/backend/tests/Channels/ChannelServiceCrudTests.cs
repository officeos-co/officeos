using OffceOs.Features.Channels.Application;
using OffceOs.Database.Models;
using OffceOs.Features.Channels.Domain;
using OffceOs.Common.Infrastructure.Security;
using OffceOs.Features.Agents.Infrastructure;
using OffceOs.Features.Channels.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Channels;

public sealed class ChannelServiceCrudTests
{
    [Fact]
    public async Task Repository_skips_legacy_unsupported_channel_connections()
    {
        await using var db = TestDbFactory.Create("channel-legacy-type");
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var supportedConnectionId = Guid.NewGuid();
        var legacyConnectionId = Guid.NewGuid();

        db.ChannelConnections.Add(new ChannelConnectionEntity
        {
            Id = supportedConnectionId,
            ChannelType = ChannelType.Telegram.ToStorageString(),
            DisplayName = "Telegram",
            Enabled = true,
            CreatedById = ownerId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        });
        db.ChannelConnections.Add(new ChannelConnectionEntity
        {
            Id = legacyConnectionId,
            ChannelType = "whatsapp",
            DisplayName = "WhatsApp",
            Enabled = true,
            CreatedById = ownerId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var repository = new ChannelRepository(db);

        var rows = await repository.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspaceId });
        var legacy = await repository.GetConnectionByAsync(new ChannelConnectionFilter { Id = legacyConnectionId, WorkspaceId = workspaceId });

        Assert.Equal(supportedConnectionId, Assert.Single(rows).Id);
        Assert.Null(legacy);
    }

    [Fact]
    public async Task Repository_skips_bindings_for_legacy_unsupported_channel_connections()
    {
        await using var db = TestDbFactory.Create("channel-legacy-binding");
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var supportedConnectionId = Guid.NewGuid();
        var legacyConnectionId = Guid.NewGuid();

        db.Agents.Add(TestAgent(agentId, ownerId, workspaceId, "Agent"));
        db.ChannelConnections.Add(new ChannelConnectionEntity
        {
            Id = supportedConnectionId,
            ChannelType = ChannelType.Telegram.ToStorageString(),
            DisplayName = "Telegram",
            Enabled = true,
            CreatedById = ownerId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        });
        db.ChannelConnections.Add(new ChannelConnectionEntity
        {
            Id = legacyConnectionId,
            ChannelType = "whatsapp",
            DisplayName = "WhatsApp",
            Enabled = true,
            CreatedById = ownerId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        });
        db.AgentChannelBindings.Add(new AgentChannelBindingEntity
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            ChannelConnectionId = supportedConnectionId,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        db.AgentChannelBindings.Add(new AgentChannelBindingEntity
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            ChannelConnectionId = legacyConnectionId,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var rows = await new ChannelRepository(db).ListBindingsAsync(agentId);

        Assert.Equal(supportedConnectionId, Assert.Single(rows).ChannelConnectionId);
    }

    [Fact]
    public async Task Connection_crud_supports_multiple_channels_of_same_kind_without_cross_updates()
    {
        await using var db = TestDbFactory.Create("channel-crud");
        var publisher = new RecordingPublisher();
        var gateway = new RecordingChannelGateway();
        var service = CreateService(db, gateway, publisher);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var telegramOps = await service.CreateConnectionAsync("telegram", "Telegram Ops", """{"token":"ops"}""", ownerId, workspaceId);
        var telegramSupport = await service.CreateConnectionAsync("telegram", "Telegram Support", """{"token":"support"}""", ownerId, workspaceId);
        var slackOps = await service.CreateConnectionAsync("slack", "Slack Ops", """{"token":"slack"}""", ownerId, workspaceId);

        var slackBeforeUpdate = await new ChannelRepository(db).GetConnectionByAsync(new ChannelConnectionFilter { Id = slackOps.Id });
        var updatedOps = await service.UpdateOwnedConnectionAsync(telegramOps.Id, ownerId, workspaceId, "Telegram Ops Renamed", null, false);
        var updatedSlack = await service.UpdateOwnedConnectionAsync(slackOps.Id, ownerId, workspaceId, null, """{"token":"slack-new"}""", null);
        var deletedSupport = await service.DeleteOwnedConnectionAsync(telegramSupport.Id, ownerId, workspaceId);
        var rows = await new ChannelRepository(db).ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspaceId });

        Assert.Equal("Telegram Ops Renamed", updatedOps.DisplayName);
        Assert.False(updatedOps.Enabled);
        Assert.True(deletedSupport);
        Assert.NotEqual(slackBeforeUpdate?.EncryptedCreds, updatedSlack.EncryptedCreds);
        Assert.Equal([telegramOps.Id, slackOps.Id], rows.Select(row => row.Id));
        Assert.DoesNotContain(rows, row => row.Id == telegramSupport.Id);
        Assert.Equal("Slack Ops", Assert.Single(rows, row => row.Id == slackOps.Id).DisplayName);
        Assert.Equal(6, gateway.ReloadCount);
    }

    [Fact]
    public async Task Binding_crud_is_connection_specific_even_when_channel_type_matches()
    {
        await using var db = TestDbFactory.Create("channel-binding-crud");
        var service = CreateService(db, new RecordingChannelGateway(), new RecordingPublisher());
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Agents.Add(TestAgent(agentId, ownerId, workspaceId, "Agent"));
        await db.SaveChangesAsync();

        var telegramOps = await service.CreateConnectionAsync("telegram", "Telegram Ops", null, ownerId, workspaceId);
        var telegramSupport = await service.CreateConnectionAsync("telegram", "Telegram Support", null, ownerId, workspaceId);

        var opsBinding = await service.BindOwnedAgentAsync(agentId, telegramOps.Id, ownerId, workspaceId, """{"platformId":"ops"}""");
        var sameOpsBinding = await service.BindOwnedAgentAsync(agentId, telegramOps.Id, ownerId, workspaceId, """{"platformId":"ignored"}""");
        var supportBinding = await service.BindOwnedAgentAsync(agentId, telegramSupport.Id, ownerId, workspaceId, """{"platformId":"support"}""");
        var updatedSupport = await service.UpdateOwnedBindingConfigAsync(agentId, telegramSupport.Id, ownerId, workspaceId, """{"platformId":"support-updated"}""");
        var unboundOps = await service.UnbindOwnedAgentAsync(agentId, telegramOps.Id, ownerId, workspaceId);
        var remainingBindings = await service.ListBindingsForOwnedAgentAsync(agentId, ownerId, workspaceId);

        Assert.Equal(opsBinding.Id, sameOpsBinding.Id);
        Assert.NotEqual(opsBinding.Id, supportBinding.Id);
        Assert.Contains("support-updated", updatedSupport.Config);
        Assert.True(unboundOps);
        var remaining = Assert.Single(remainingBindings);
        Assert.Equal(telegramSupport.Id, remaining.ChannelConnectionId);
    }

    [Fact]
    public async Task Owned_binding_crud_rejects_cross_workspace_agent_or_connection()
    {
        await using var db = TestDbFactory.Create("channel-binding-owned");
        var service = CreateService(db, new RecordingChannelGateway(), new RecordingPublisher());
        var ownerId = Guid.NewGuid();
        var workspaceOne = Guid.NewGuid();
        var workspaceTwo = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Agents.Add(TestAgent(agentId, ownerId, workspaceOne, "Agent"));
        await db.SaveChangesAsync();

        var otherWorkspaceTelegram = await service.CreateConnectionAsync("telegram", "Other Telegram", null, ownerId, workspaceTwo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BindOwnedAgentAsync(agentId, otherWorkspaceTelegram.Id, ownerId, workspaceOne, null));
    }

    private static ChannelService CreateService(
        OffceOs.Database.EaosDbContext db,
        RecordingChannelGateway gateway,
        RecordingPublisher publisher)
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-channel-test-keys-{Guid.NewGuid():N}");
        return new ChannelService(
            new ChannelRepository(db),
            gateway,
            new AgentRepository(db),
            new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath))),
            publisher,
            new ChannelReplyContext(),
            new FakeResourceLogWriterService());
    }

    private static AgentEntity TestAgent(Guid agentId, Guid ownerId, Guid workspaceId, string name) => new()
    {
        Id = agentId,
        Name = name,
        Provider = "openai",
        Model = "gpt-4o-mini",
        Status = "running",
        CreatedAt = DateTime.UtcNow,
        OwnerId = ownerId,
        WorkspaceId = workspaceId,
    };
}

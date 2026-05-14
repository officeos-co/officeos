using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Channels;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Channels;

public sealed class ChannelConnectionRepositoryTests
{
    [Fact]
    public async Task ListConnections_allows_multiple_connections_with_same_channel_type_and_scopes_by_workspace()
    {
        await using var db = TestDbFactory.Create("channel-repository");
        var repository = new ChannelRepository(db);
        var ownerId = Guid.NewGuid();
        var workspaceOne = Guid.NewGuid();
        var workspaceTwo = Guid.NewGuid();

        var telegramOps = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspaceOne));
        var telegramSupport = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Support", ownerId, workspaceOne));
        var slackOps = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Slack, "Slack Ops", ownerId, workspaceOne));
        await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Other Workspace Telegram", ownerId, workspaceTwo));

        var workspaceOneRows = await repository.ListConnectionsAsync(new ChannelConnectionFilter
        {
            WorkspaceId = workspaceOne,
        });
        var telegramRows = await repository.ListConnectionsAsync(new ChannelConnectionFilter
        {
            WorkspaceId = workspaceOne,
            ChannelType = ChannelType.Telegram.ToStorageString(),
        });

        Assert.Equal([telegramOps.Id, telegramSupport.Id, slackOps.Id], workspaceOneRows.Select(row => row.Id));
        Assert.Equal([telegramOps.Id, telegramSupport.Id], telegramRows.Select(row => row.Id));
    }

    [Fact]
    public async Task DeleteConnection_removes_only_bindings_for_that_connection()
    {
        await using var db = TestDbFactory.Create("channel-delete");
        var repository = new ChannelRepository(db);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentOneId = Guid.NewGuid();
        var agentTwoId = Guid.NewGuid();
        db.Agents.AddRange(
            TestAgent(agentOneId, ownerId, workspaceId, "Agent One"),
            TestAgent(agentTwoId, ownerId, workspaceId, "Agent Two"));
        await db.SaveChangesAsync();

        var telegramOps = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspaceId));
        var telegramSupport = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Support", ownerId, workspaceId));

        await repository.CreateBindingAsync(new AgentChannelBindingRecord
        {
            AgentId = agentOneId,
            ChannelConnectionId = telegramOps.Id,
        });
        await repository.CreateBindingAsync(new AgentChannelBindingRecord
        {
            AgentId = agentTwoId,
            ChannelConnectionId = telegramSupport.Id,
        });

        Assert.True(await repository.DeleteConnectionAsync(telegramOps.Id));

        Assert.Empty(await repository.FindBindingsByConnectionAsync(telegramOps.Id));
        var supportBindings = await repository.FindBindingsByConnectionAsync(telegramSupport.Id);
        var supportConnection = await repository.GetConnectionByAsync(new ChannelConnectionFilter { Id = telegramSupport.Id });
        Assert.Single(supportBindings);
        Assert.Equal(agentTwoId, supportBindings[0].AgentId);
        Assert.NotNull(supportConnection);
    }

    [Fact]
    public async Task CreateBinding_is_idempotent_for_agent_and_channel_connection()
    {
        await using var db = TestDbFactory.Create("channel-binding-idempotent");
        var repository = new ChannelRepository(db);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Agents.Add(TestAgent(agentId, ownerId, workspaceId, "Agent"));
        await db.SaveChangesAsync();

        var telegram = await repository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspaceId));

        var created = await repository.CreateBindingAsync(new AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = telegram.Id,
        });
        var duplicate = await repository.CreateBindingAsync(new AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = telegram.Id,
        });

        var bindings = await repository.ListBindingsAsync(agentId);
        var binding = Assert.Single(bindings);
        Assert.Equal(created.Id, duplicate.Id);
        Assert.Equal(created.Id, binding.Id);
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

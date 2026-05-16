using OffceOs.Database.Models;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.ResourceLogs;
using OffceOs.Infrastructure.Features.ResourceLogs;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Channels;

public sealed class ChannelLogIsolationTests
{
    [Fact]
    public async Task Channel_logs_are_isolated_by_connection_id_for_same_kind_channels()
    {
        await using var db = TestDbFactory.Create("channel-log-isolation");
        var channelRepository = new ChannelRepository(db);
        var logRepository = new ResourceLogRepository(db);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = "running",
            CreatedAt = DateTime.UtcNow,
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
        });
        await db.SaveChangesAsync();
        var telegramOps = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspaceId));
        var telegramSupport = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Support", ownerId, workspaceId));

        await logRepository.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            Type = ResourceLogType.ChannelIn,
            Channel = "telegram",
            ChannelConnectionId = telegramOps.Id,
            Content = "ops",
            CorrelationId = "ops-correlation",
        });
        await logRepository.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            Type = ResourceLogType.ChannelIn,
            Channel = "telegram",
            ChannelConnectionId = telegramSupport.Id,
            Content = "support",
            CorrelationId = "support-correlation",
        });

        var opsLogs = await logRepository.ListAsync(new ResourceLogFilter { ChannelConnectionId = telegramOps.Id });
        var supportLogs = await logRepository.ListAsync(new ResourceLogFilter { ChannelConnectionId = telegramSupport.Id });

        var opsLog = Assert.Single(opsLogs);
        var supportLog = Assert.Single(supportLogs);
        Assert.Equal("ops", opsLog.Content);
        Assert.Equal("support", supportLog.Content);
        Assert.Equal(workspaceId, opsLog.WorkspaceId);
        Assert.Equal(workspaceId, supportLog.WorkspaceId);
    }
}

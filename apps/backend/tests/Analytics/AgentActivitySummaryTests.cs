using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Application.Features.Analytics;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Analytics;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Analytics;

public sealed class AgentActivitySummaryTests
{
    [Fact]
    public async Task Last_relevant_agent_message_skips_pod_startup_logs()
    {
        await using var db = TestDbFactory.Create("agent-activity-summary");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId);
        var service = CreateService(db);

        await service.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.MessageIn,
            Content = "Please check deployment",
            Time = DateTime.UtcNow.AddSeconds(-2),
        });
        await service.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.System,
            Content = "Pod connected",
            Time = DateTime.UtcNow,
        });

        var message = await service.GetLastRelevantMessageForAgentAsync(agentId, workspaceId);

        Assert.Equal("Please check deployment", message);
    }

    [Fact]
    public async Task Last_relevant_channel_message_uses_channel_scope()
    {
        await using var db = TestDbFactory.Create("channel-activity-summary");
        var workspaceId = Guid.NewGuid();
        var channelConnectionId = Guid.NewGuid();
        var otherChannelConnectionId = Guid.NewGuid();
        var service = CreateService(db);

        await service.AppendAsync(new AgentLogRecord
        {
            WorkspaceId = workspaceId,
            ChannelConnectionId = otherChannelConnectionId,
            Type = AgentLogType.ChannelIn,
            Channel = "slack",
            Content = "Other channel",
            Time = DateTime.UtcNow.AddSeconds(1),
        });
        await service.AppendAsync(new AgentLogRecord
        {
            WorkspaceId = workspaceId,
            ChannelConnectionId = channelConnectionId,
            Type = AgentLogType.ChannelOut,
            Channel = "slack",
            Content = "Deploy is fixed",
            Time = DateTime.UtcNow,
        });

        var message = await service.GetLastRelevantMessageForChannelConnectionAsync(channelConnectionId, workspaceId);

        Assert.Equal("Deploy is fixed", message);
    }

    [Fact]
    public async Task Tool_results_are_formatted_as_meaningful_activity()
    {
        await using var db = TestDbFactory.Create("tool-activity-summary");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId);
        var service = CreateService(db);

        await service.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.ToolResult,
            Tool = "shell",
            Content = "build passed",
            Time = DateTime.UtcNow,
        });

        var message = await service.GetLastRelevantMessageForAgentAsync(agentId, workspaceId);

        Assert.Equal("shell finished: build passed", message);
    }

    [Fact]
    public async Task Normal_agent_logs_exclude_pod_startup_logs()
    {
        await using var db = TestDbFactory.Create("normal-agent-log-filter");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId);
        var service = CreateService(db);

        await service.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.System,
            Content = "Pod connected",
            Time = DateTime.UtcNow.AddSeconds(-1),
        });
        await service.AppendAsync(new AgentLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.MessageOut,
            Content = "Ready",
            Time = DateTime.UtcNow,
        });

        var logs = await service.AgentLogs(agentId, workspaceId).ToListAsync();

        var log = Assert.Single(logs);
        Assert.Equal("Ready", log.Content);
    }

    private static AgentLogService CreateService(EaosDbContext db) =>
        new(
            new AgentLogRepository(db),
            new AgentRepository(db),
            new NoopPublisher(),
            null!,
            NullLogger<AgentLogService>.Instance);

    private static async Task AddAgentAsync(EaosDbContext db, Guid agentId, Guid workspaceId)
    {
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = AgentStatus.Idle.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
            WorkspaceId = workspaceId,
        });
        await db.SaveChangesAsync();
    }
}

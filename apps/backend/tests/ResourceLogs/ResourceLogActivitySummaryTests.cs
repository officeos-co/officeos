using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.ControlPlane.Application;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Features.ControlPlane.Domain;
using OffceOs.Features.ResourceLogs.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.ResourceLogs;

public sealed class ResourceLogActivitySummaryTests
{
    [Fact]
    public async Task Last_relevant_agent_message_skips_pod_startup_logs()
    {
        await using var db = TestDbFactory.Create("agent-activity-summary");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId);
        var service = CreateService(db);

        await service.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = ResourceLogType.MessageIn,
            Content = "Please check deployment",
            Time = DateTime.UtcNow.AddSeconds(-2),
        });
        await service.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = ResourceLogType.System,
            Content = "Pod connected",
            Time = DateTime.UtcNow,
        });

        var messages = await service.GetLastRelevantMessagesAsync(
            new LastRelevantLogQueryRequest(AgentIds: [agentId], WorkspaceId: workspaceId));
        var message = messages[agentId];

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

        await service.AppendAsync(new ResourceLogRecord
        {
            WorkspaceId = workspaceId,
            ChannelConnectionId = otherChannelConnectionId,
            Type = ResourceLogType.ChannelIn,
            Channel = "slack",
            Content = "Other channel",
            Time = DateTime.UtcNow.AddSeconds(1),
        });
        await service.AppendAsync(new ResourceLogRecord
        {
            WorkspaceId = workspaceId,
            ChannelConnectionId = channelConnectionId,
            Type = ResourceLogType.ChannelOut,
            Channel = "slack",
            Content = "Deploy is fixed",
            Time = DateTime.UtcNow,
        });

        var messages = await service.GetLastRelevantMessagesAsync(
            new LastRelevantLogQueryRequest(ChannelConnectionIds: [channelConnectionId], WorkspaceId: workspaceId));
        var message = messages[channelConnectionId];

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

        await service.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = ResourceLogType.ToolResult,
            Tool = "shell",
            Content = "build passed",
            Time = DateTime.UtcNow,
        });

        var messages = await service.GetLastRelevantMessagesAsync(
            new LastRelevantLogQueryRequest(AgentIds: [agentId], WorkspaceId: workspaceId));
        var message = messages[agentId];

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

        await service.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = ResourceLogType.System,
            Content = "Pod connected",
            Time = DateTime.UtcNow.AddSeconds(-1),
        });
        await service.AppendAsync(new ResourceLogRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = ResourceLogType.MessageOut,
            Content = "Ready",
            Time = DateTime.UtcNow,
        });

        var page = await service.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: workspaceId,
            AgentId: agentId,
            Sort: ResourceLogSort.TimeAscending));

        var log = Assert.Single(page.Items);
        Assert.Equal("Ready", log.Content);
    }

    private static ResourceLogService CreateService(EaosDbContext db) =>
        new(new ResourceLogRepository(db), new FakeControlPlaneResourceCatalogService());

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

    private sealed class FakeControlPlaneResourceCatalogService : IControlPlaneResourceCatalogService
    {
        public IReadOnlyList<ControlPlaneResourceDescriptor> List() => ControlPlaneResourceRegistry.Resources;

        public ControlPlaneResourceDescriptor? Find(string kindOrAlias)
        {
            return ControlPlaneResourceRegistry.Resources.FirstOrDefault(resource =>
                string.Equals(resource.Kind, kindOrAlias, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resource.Singular, kindOrAlias, StringComparison.OrdinalIgnoreCase) ||
                resource.Aliases.Any(alias => string.Equals(alias, kindOrAlias, StringComparison.OrdinalIgnoreCase)));
        }
    }
}

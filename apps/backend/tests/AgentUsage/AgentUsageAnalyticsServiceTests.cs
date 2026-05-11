using OffceOs.Application.Features.AgentUsage;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.AgentUsage;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Infrastructure.Features.AgentUsage;
using OffceOs.Infrastructure.Features.Analytics;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.AgentUsage;

public sealed class AgentUsageAnalyticsServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_returns_model_activity_context_and_tool_breakdowns()
    {
        await using var db = TestDbFactory.Create("agent-usage-dashboard");
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        SeedScope(db, ownerId, workspaceId, agentId);
        var usageRepository = new AgentUsageRepository(db);
        var logRepository = new AgentLogRepository(db);
        var service = new AgentUsageAnalyticsService(usageRepository, logRepository);
        var now = DateTime.UtcNow.Date.AddHours(10);

        await usageRepository.SaveAsync(new AgentUsageCallRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            CorrelationId = "corr-1",
            Time = now,
            Provider = "openai",
            Model = "gpt-4o-mini",
            InputTokens = 100,
            OutputTokens = 25,
            CacheReadTokens = 50,
            Credits = 125,
            Activity = AgentUsageActivityKinds.Coding,
            ContextParts =
            [
                new AgentUsageContextPartRecord
                {
                    Kind = AgentUsageContextPartKinds.ToolSchema,
                    Label = "file_edit",
                    Tool = "file_edit",
                    Tokens = 40,
                    CharacterCount = 160,
                },
            ],
        });
        await logRepository.AppendAsync(AgentLogRecord.ToolCallEntry(agentId, "file_read", """{"path":"src/App.cs"}""", "corr-1", now));
        await logRepository.AppendAsync(AgentLogRecord.ToolCallEntry(agentId, "shell", """{"command":"git status --short"}""", "corr-1", now.AddSeconds(1)));

        var result = await service.GetDashboardAsync(ownerId, new AgentUsageAnalyticsRequest(now, now, workspaceId));

        Assert.Equal(125, result.TotalTokens);
        Assert.Equal(50, result.CacheReadTokens);
        Assert.Contains(result.ByModel, item => item.Name == "gpt-4o-mini" && item.Calls == 1);
        Assert.Contains(result.ByActivity, item => item.Name == AgentUsageActivityKinds.Coding);
        Assert.Contains(result.CoreTools, item => item.Name == "file_read");
        Assert.Contains(result.ShellCommands, item => item.Name == "git");
    }

    [Fact]
    public async Task GetOptimizeAsync_flags_repeated_reads_and_schema_heavy_context()
    {
        await using var db = TestDbFactory.Create("agent-usage-optimize");
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        SeedScope(db, ownerId, workspaceId, agentId);
        var usageRepository = new AgentUsageRepository(db);
        var logRepository = new AgentLogRepository(db);
        var service = new AgentUsageAnalyticsService(usageRepository, logRepository);
        var now = DateTime.UtcNow.Date.AddHours(10);

        await usageRepository.SaveAsync(new AgentUsageCallRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            OwnerId = ownerId,
            CorrelationId = "corr-1",
            Time = now,
            Provider = "openai",
            Model = "gpt-4o-mini",
            InputTokens = 1000,
            OutputTokens = 20,
            Credits = 1020,
            Activity = AgentUsageActivityKinds.Coding,
            ContextParts =
            [
                new AgentUsageContextPartRecord
                {
                    Kind = AgentUsageContextPartKinds.ToolSchema,
                    Label = "large schema",
                    Tokens = 600,
                    CharacterCount = 2400,
                },
            ],
        });

        for (var i = 0; i < 4; i++)
        {
            await logRepository.AppendAsync(AgentLogRecord.ToolCallEntry(agentId, "file_read", """{"path":"src/App.cs"}""", "corr-1", now.AddSeconds(i)));
        }
        await logRepository.AppendAsync(AgentLogRecord.ToolCallEntry(agentId, "file_edit", """{"path":"src/App.cs"}""", "corr-1", now.AddSeconds(10)));

        var result = await service.GetOptimizeAsync(ownerId, new AgentUsageAnalyticsRequest(now, now, workspaceId));

        Assert.Contains(result.Findings, finding => finding.Title == "Agents re-read the same files");
        Assert.Contains(result.Findings, finding => finding.Title == "Tool schemas dominate context");
        Assert.True(result.EstimatedTokenSavings > 0);
    }

    private static void SeedScope(EaosDbContext db, Guid ownerId, Guid workspaceId, Guid agentId)
    {
        db.Users.Add(new UserEntity
        {
            Id = ownerId,
            Email = "owner@example.com",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OwnerUserId = ownerId,
            Name = "Workspace",
            OwnerKind = "personal",
        });
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
            Status = "idle",
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();
    }
}

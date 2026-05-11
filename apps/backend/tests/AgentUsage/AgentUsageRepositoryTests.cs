using Microsoft.EntityFrameworkCore;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.AgentUsage;
using OffceOs.Infrastructure.Features.AgentUsage;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.AgentUsage;

public sealed class AgentUsageRepositoryTests
{
    [Fact]
    public async Task SaveAsync_enriches_owner_workspace_and_persists_context_parts()
    {
        await using var db = TestDbFactory.Create("agent-usage-repository");
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        SeedScope(db, ownerId, workspaceId, agentId);
        var repository = new AgentUsageRepository(db);

        await repository.SaveAsync(new AgentUsageCallRecord
        {
            AgentId = agentId,
            CorrelationId = "corr-1",
            Provider = "openai",
            Model = "gpt-4o-mini",
            InputTokens = 100,
            OutputTokens = 20,
            Credits = 120,
            Activity = AgentUsageActivityKinds.Coding,
            ContextParts =
            [
                new AgentUsageContextPartRecord
                {
                    Kind = AgentUsageContextPartKinds.UserMessage,
                    Label = "user message",
                    Role = "user",
                    Tokens = 60,
                    CharacterCount = 240,
                },
            ],
        });

        var rows = await repository.ListAsync(new AgentUsageFilter { OwnerId = ownerId, Model = "gpt-4o-mini" });

        var row = Assert.Single(rows);
        Assert.Equal(workspaceId, row.WorkspaceId);
        Assert.Equal(ownerId, row.OwnerId);
        var part = Assert.Single(row.ContextParts);
        Assert.Equal(row.Id, part.CallId);
        Assert.Equal(AgentUsageContextPartKinds.UserMessage, part.Kind);
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

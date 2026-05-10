using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Management;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Events;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Management;
using OffceOs.EventHandlers.Features.Management;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class OrganizationAuditLogTests
{
    [Fact]
    public async Task Organization_mutations_publish_events_only_after_successful_writes()
    {
        await using var db = TestDbFactory.Create("audit-publish");
        var ownerId = Guid.NewGuid();
        var organizationId = await SeedOrganizationAsync(db, ownerId);
        var publisher = new RecordingPublisher();
        var service = new OrganizationService(
            new OrganizationRepository(db),
            new WorkspaceRepository(db),
            new WorkspaceMemberRepository(db),
            publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InviteMemberAsync(ownerId, "owner@example.com", "Owner", "not-an-email", "Editor"));
        Assert.Empty(publisher.Notifications);

        var member = await service.InviteMemberAsync(
            ownerId,
            "owner@example.com",
            "Owner",
            "new.member@example.com",
            "Admin");

        var saved = await db.OrgMembers.FindAsync(member.Id);
        var notification = Assert.IsType<OrganizationMemberInvitedEvent>(Assert.Single(publisher.Notifications));
        Assert.NotNull(saved);
        Assert.Equal(organizationId, notification.OrganizationId);
        Assert.Equal(ownerId, notification.ActorUserId);
        Assert.Equal(member.Id, notification.MemberId);
    }

    [Fact]
    public async Task AuditLogHandler_maps_admin_and_runtime_events_to_audit_records()
    {
        await using var db = TestDbFactory.Create("audit-handler");
        var ownerId = Guid.NewGuid();
        var organizationId = await SeedOrganizationAsync(db, ownerId);
        var workspaceId = await SeedWorkspaceAsync(db, organizationId);
        var agentId = await SeedAgentAsync(db, ownerId, workspaceId);
        var repository = new OrganizationAuditLogRepository(db);
        var handler = new AuditLogHandler(repository, new AgentRepository(db), new WorkspaceRepository(db));

        await handler.Handle(new OrganizationRenamedEvent(organizationId, ownerId, "Old", "New"), CancellationToken.None);
        await handler.Handle(new OrganizationMemberInvitedEvent(organizationId, ownerId, Guid.NewGuid(), "member@example.com", "Admin"), CancellationToken.None);
        await handler.Handle(new OrganizationMemberInviteAcceptedEvent(organizationId, ownerId, Guid.NewGuid(), "member@example.com", "Admin"), CancellationToken.None);
        await handler.Handle(new OrganizationMemberRemovedEvent(organizationId, ownerId, Guid.NewGuid(), Guid.NewGuid(), "member@example.com", "Editor"), CancellationToken.None);
        await handler.Handle(new OrganizationWorkspaceCreatedEvent(organizationId, ownerId, workspaceId, "Ops"), CancellationToken.None);
        await handler.Handle(new WorkspaceUpdatedEvent(organizationId, ownerId, workspaceId, "Ops", "Platform"), CancellationToken.None);
        await handler.Handle(new WorkspaceDeletedEvent(organizationId, ownerId, workspaceId, "Platform"), CancellationToken.None);
        await handler.Handle(new WorkspaceMemberAddedEvent(organizationId, ownerId, workspaceId, Guid.NewGuid(), "Viewer"), CancellationToken.None);
        await handler.Handle(new WorkspaceMemberRoleUpdatedEvent(organizationId, ownerId, workspaceId, Guid.NewGuid(), "Viewer", "Editor"), CancellationToken.None);
        await handler.Handle(new WorkspaceMemberRemovedEvent(organizationId, ownerId, workspaceId, Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new WorkspaceOrganizationGrantCreatedEvent(organizationId, ownerId, workspaceId, Guid.NewGuid(), "Viewer"), CancellationToken.None);
        await handler.Handle(new WorkspaceOrganizationGrantRevokedEvent(organizationId, ownerId, workspaceId, Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new AccessGroupCreatedEvent(organizationId, ownerId, Guid.NewGuid(), "Finance"), CancellationToken.None);
        await handler.Handle(new AccessGroupRenamedEvent(organizationId, ownerId, Guid.NewGuid(), "Finance", "Ops"), CancellationToken.None);
        await handler.Handle(new AccessGroupDeletedEvent(organizationId, ownerId, Guid.NewGuid(), "Ops"), CancellationToken.None);
        await handler.Handle(new AccessGroupMemberAddedEvent(organizationId, ownerId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new AccessGroupMemberRemovedEvent(organizationId, ownerId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new AccessGroupWorkspaceGrantCreatedEvent(organizationId, ownerId, Guid.NewGuid(), workspaceId, "Viewer"), CancellationToken.None);
        await handler.Handle(new AccessGroupWorkspaceGrantRevokedEvent(organizationId, ownerId, Guid.NewGuid(), workspaceId), CancellationToken.None);
        await handler.Handle(new OrganizationPolicyProfileUpdatedEvent(organizationId, ownerId, false, true, false, true, 1, 2, 3, 4), CancellationToken.None);
        await handler.Handle(new OrganizationProviderProfileSavedEvent(organizationId, ownerId, "openai", "OpenAI", "apiKey", 2, true), CancellationToken.None);
        await handler.Handle(new OrganizationProviderProfileDeletedEvent(organizationId, ownerId, "openai"), CancellationToken.None);
        await handler.Handle(new LlmCallCompletedEvent(agentId, "corr-1", "openai", "gpt-4o-mini", 123, 10, 20), CancellationToken.None);
        await handler.Handle(new ToolCallCompletedEvent(agentId, "corr-1", "shell", false, "blocked", 17), CancellationToken.None);
        await handler.Handle(new AgentToolPolicyDeniedEvent(agentId, "corr-2", "file_write", "file write tools are disabled by organization policy"), CancellationToken.None);

        var rows = await repository.ListAsync(new OrganizationAuditLogFilter { OrganizationId = organizationId, Limit = 100 });
        var byAction = rows.ToDictionary(row => row.Action);
        var expected = new Dictionary<string, (string ResourceType, string Outcome)>
        {
            [OrganizationAuditKinds.OrganizationRenamed] = (OrganizationAuditKinds.Organization, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.OrganizationMemberInvited] = (OrganizationAuditKinds.OrganizationMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.OrganizationMemberInviteAccepted] = (OrganizationAuditKinds.OrganizationMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.OrganizationMemberRemoved] = (OrganizationAuditKinds.OrganizationMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceCreated] = (OrganizationAuditKinds.Workspace, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceUpdated] = (OrganizationAuditKinds.Workspace, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceDeleted] = (OrganizationAuditKinds.Workspace, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceMemberAdded] = (OrganizationAuditKinds.WorkspaceMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceMemberRoleUpdated] = (OrganizationAuditKinds.WorkspaceMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceMemberRemoved] = (OrganizationAuditKinds.WorkspaceMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceOrganizationGrantCreated] = (OrganizationAuditKinds.WorkspaceOrganizationGrant, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.WorkspaceOrganizationGrantRevoked] = (OrganizationAuditKinds.WorkspaceOrganizationGrant, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupCreated] = (OrganizationAuditKinds.AccessGroup, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupRenamed] = (OrganizationAuditKinds.AccessGroup, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupDeleted] = (OrganizationAuditKinds.AccessGroup, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupMemberAdded] = (OrganizationAuditKinds.AccessGroupMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupMemberRemoved] = (OrganizationAuditKinds.AccessGroupMember, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupWorkspaceGrantCreated] = (OrganizationAuditKinds.AccessGroupWorkspaceGrant, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AccessGroupWorkspaceGrantRevoked] = (OrganizationAuditKinds.AccessGroupWorkspaceGrant, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.OrganizationPolicyUpdated] = (OrganizationAuditKinds.OrganizationPolicy, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.ProviderProfileSaved] = (OrganizationAuditKinds.ProviderProfile, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.ProviderProfileDeleted] = (OrganizationAuditKinds.ProviderProfile, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AgentProviderModelUsed] = (OrganizationAuditKinds.Agent, OrganizationAuditKinds.Success),
            [OrganizationAuditKinds.AgentToolUsed] = (OrganizationAuditKinds.Tool, OrganizationAuditKinds.Failure),
            [OrganizationAuditKinds.AgentToolPolicyDenied] = (OrganizationAuditKinds.Tool, OrganizationAuditKinds.Denied),
        };

        Assert.Equal(25, rows.Count);
        foreach (var (action, expectation) in expected)
        {
            var row = byAction[action];
            Assert.Equal(organizationId, row.OrganizationId);
            Assert.Equal(ownerId, row.ActorUserId);
            Assert.Equal(expectation.ResourceType, row.ResourceType);
            Assert.Equal(expectation.Outcome, row.Outcome);
        }

        Assert.Equal(workspaceId, byAction[OrganizationAuditKinds.WorkspaceUpdated].WorkspaceId);
        Assert.Contains("\"authKind\":\"apiKey\"", byAction[OrganizationAuditKinds.ProviderProfileSaved].MetadataJson);
        Assert.Equal(agentId, byAction[OrganizationAuditKinds.AgentProviderModelUsed].AgentId);
        Assert.Contains("\"provider\":\"openai\"", byAction[OrganizationAuditKinds.AgentProviderModelUsed].MetadataJson);
    }

    [Fact]
    public async Task Repository_filters_cover_audit_query_dimensions()
    {
        await using var db = TestDbFactory.Create("audit-filters");
        var organizationId = await SeedOrganizationAsync(db, Guid.NewGuid());
        var otherOrganizationId = await SeedOrganizationAsync(db, Guid.NewGuid(), "other@example.com");
        var actorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var repository = new OrganizationAuditLogRepository(db);
        var firstTime = new DateTime(2026, 05, 10, 10, 0, 0, DateTimeKind.Utc);

        var matching = await repository.SaveAsync(new OrganizationAuditLogRecord
        {
            OrganizationId = organizationId,
            ActorUserId = actorId,
            WorkspaceId = workspaceId,
            AgentId = agentId,
            Action = OrganizationAuditKinds.AgentToolPolicyDenied,
            ResourceType = OrganizationAuditKinds.Tool,
            ResourceId = "shell",
            Outcome = OrganizationAuditKinds.Denied,
            MetadataJson = """{"needle":"finance export"}""",
            OccurredAt = firstTime,
        });
        await repository.SaveAsync(new OrganizationAuditLogRecord
        {
            OrganizationId = organizationId,
            ActorUserId = Guid.NewGuid(),
            Action = OrganizationAuditKinds.OrganizationRenamed,
            ResourceType = OrganizationAuditKinds.Organization,
            Outcome = OrganizationAuditKinds.Success,
            MetadataJson = """{"name":"other"}""",
            OccurredAt = firstTime.AddDays(-5),
        });
        await repository.SaveAsync(new OrganizationAuditLogRecord
        {
            OrganizationId = otherOrganizationId,
            Action = OrganizationAuditKinds.AgentToolPolicyDenied,
            ResourceType = OrganizationAuditKinds.Tool,
            Outcome = OrganizationAuditKinds.Denied,
            MetadataJson = """{"needle":"finance export"}""",
            OccurredAt = firstTime,
        });

        var rows = await repository.ListAsync(new OrganizationAuditLogFilter
        {
            OrganizationId = organizationId,
            From = firstTime.AddMinutes(-1),
            To = firstTime.AddMinutes(1),
            Action = OrganizationAuditKinds.AgentToolPolicyDenied,
            ActorUserId = actorId,
            WorkspaceId = workspaceId,
            AgentId = agentId,
            Outcome = OrganizationAuditKinds.Denied,
            Search = "finance",
        });

        var row = Assert.Single(rows);
        Assert.Equal(matching.Id, row.Id);
    }

    [Fact]
    public async Task Audit_query_and_export_deny_non_admin_users()
    {
        await using var db = TestDbFactory.Create("audit-auth");
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var organizationId = await SeedOrganizationAsync(db, ownerId);
        db.Users.Add(new UserEntity { Id = memberId, Email = "member@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = memberId,
            Email = "member@example.com",
            Role = OrgRole.Editor.ToString(),
            Status = MemberStatus.Active.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var service = new OrganizationAuditLogService(new OrganizationAuditLogRepository(db), new OrganizationRepository(db));
        var filter = new OrganizationAuditLogFilter { OrganizationId = organizationId };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListAsync(memberId, filter));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(memberId, filter, "csv"));
    }

    [Fact]
    public async Task Audit_export_is_deterministic_and_redacts_secret_like_metadata()
    {
        await using var db = TestDbFactory.Create("audit-export");
        var ownerId = Guid.NewGuid();
        var organizationId = await SeedOrganizationAsync(db, ownerId);
        var repository = new OrganizationAuditLogRepository(db);
        var service = new OrganizationAuditLogService(repository, new OrganizationRepository(db));
        var occurredAt = new DateTime(2026, 05, 10, 12, 30, 0, DateTimeKind.Utc);

        await repository.SaveAsync(new OrganizationAuditLogRecord
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OrganizationId = organizationId,
            ActorUserId = ownerId,
            Action = OrganizationAuditKinds.ProviderProfileSaved,
            ResourceType = OrganizationAuditKinds.ProviderProfile,
            ResourceId = "openai",
            Outcome = OrganizationAuditKinds.Success,
            MetadataJson = """{"z":"last","apiKey":"secret-value","nested":{"password":"p","alpha":"first"}}""",
            OccurredAt = occurredAt,
        });

        var csv1 = await service.ExportAsync(ownerId, new OrganizationAuditLogFilter { OrganizationId = organizationId }, "csv");
        var csv2 = await service.ExportAsync(ownerId, new OrganizationAuditLogFilter { OrganizationId = organizationId }, "csv");
        var jsonl = await service.ExportAsync(ownerId, new OrganizationAuditLogFilter { OrganizationId = organizationId }, "jsonl");

        Assert.Equal(csv1.Content, csv2.Content);
        Assert.DoesNotContain("secret-value", csv1.Content);
        Assert.DoesNotContain("\"p\"", jsonl.Content);
        Assert.Contains("[redacted]", csv1.Content);
        Assert.Contains("\"alpha\":\"first\"", jsonl.Content);
        Assert.StartsWith("id,occurredAt,organizationId,actorUserId,workspaceId,agentId,action,resourceType,resourceId,outcome,correlationId,metadata", csv1.Content);
    }

    private static async Task<Guid> SeedOrganizationAsync(
        EaosDbContext db,
        Guid ownerId,
        string email = "owner@example.com")
    {
        var organizationId = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = ownerId, Email = email, Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = ownerId, CreatedAt = DateTime.UtcNow });
        db.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = ownerId,
            Email = email,
            Role = OrgRole.Owner.ToString(),
            Status = MemberStatus.Active.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return organizationId;
    }

    private static async Task<Guid> SeedWorkspaceAsync(EaosDbContext db, Guid organizationId)
    {
        var workspaceId = Guid.NewGuid();
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OrganizationId = organizationId,
            OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
            Name = "Ops",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return workspaceId;
    }

    private static async Task<Guid> SeedAgentAsync(EaosDbContext db, Guid ownerId, Guid workspaceId)
    {
        var agentId = Guid.NewGuid();
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
            Name = "Runtime Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = AgentStatus.Running.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return agentId;
    }
}

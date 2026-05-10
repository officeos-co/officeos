using OffceOs.Application.Features.Agents;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Integrations;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class WorkspaceOrganizationEndToEndTests
{
    [Fact]
    public async Task Personal_workspace_lifecycle_supports_delete_recreate_and_agent_scoping()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var userId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, userId, "owner@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var personalDefault = await harness.Workspaces.GetCurrentAsync(userId);
        var scratch = await harness.Workspaces.CreateAsync(userId, "Scratch");

        await harness.Workspaces.SwitchAsync(userId, scratch.Id);
        Assert.True(await harness.Workspaces.DeleteAsync(userId, scratch.Id));

        var replacement = await harness.Workspaces.CreateAsync(userId, "Automation");
        await harness.Workspaces.SwitchAsync(userId, replacement.Id);

        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                Name: "Workspace Agent",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: null,
                IntegrationSlugs: null,
                ChannelConnectionIds: null,
                ToolNames: null,
                Resources: null,
                BootstrapMessage: null),
            userId,
            replacement.Id);

        var replacementAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = replacement.Id });
        var defaultAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = personalDefault.Id });
        var workspaces = await harness.Workspaces.ListAsync(userId);

        Assert.Contains(workspaces, workspace => workspace.Id == personalDefault.Id && workspace.OwnerKind == WorkspaceOwnerKind.Personal);
        Assert.DoesNotContain(workspaces, workspace => workspace.Id == scratch.Id);
        Assert.Single(replacementAgents);
        Assert.Equal(agent.Id, replacementAgents[0].Id);
        Assert.Empty(defaultAgents);
    }

    [Fact]
    public async Task Organization_flow_supports_roles_org_workspace_agents_and_workspace_wide_integrations()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        await WorkspaceTestHarness.SeedUserAsync(db, memberId, "member@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var orgDefault = await harness.Workspaces.GetCurrentAsync(ownerId);
        var invite = await harness.Organizations.InviteMemberAsync(
            ownerId,
            "owner@example.com",
            "Owner",
            "member@example.com",
            "Editor");
        var member = await harness.Organizations.AcceptInviteAsync(memberId, "member@example.com", invite.Id);

        var memberWorkspaces = await harness.Workspaces.ListAsync(memberId);
        var memberOrgDefault = Assert.Single(memberWorkspaces, workspace => workspace.OrganizationId == overview.Organization.Id);

        var opsWorkspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Ops");
        var seededOpsWorkspace = Assert.Single(await harness.Workspaces.ListAsync(memberId), workspace => workspace.Id == opsWorkspace.Id);
        var opsMembership = await harness.Workspaces.AddMemberAsync(ownerId, opsWorkspace.Id, memberId, "Viewer");
        var upgradedMembership = await harness.Workspaces.UpdateMemberRoleAsync(ownerId, opsWorkspace.Id, memberId, "Editor");
        await harness.Workspaces.SwitchAsync(memberId, opsWorkspace.Id);

        await harness.Integrations.RegisterAsync(ownerId, opsWorkspace.Id, WorkspaceTestHarness.CustomIntegration());
        await harness.Integrations.SaveCredentialAsync(ownerId, opsWorkspace.Id, "org-docs", new() { ["API_KEY"] = "secret" });

        var memberVisibleIntegration = await harness.Integrations.GetAsync(memberId, "org-docs", opsWorkspace.Id);
        var memberPersonalWorkspace = Assert.Single(memberWorkspaces, workspace => workspace.OwnerKind == WorkspaceOwnerKind.Personal);
        var personalIntegration = await harness.Integrations.GetAsync(memberId, "org-docs", memberPersonalWorkspace.Id);

        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                Name: "Org Agent",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: null,
                IntegrationSlugs: null,
                ChannelConnectionIds: null,
                ToolNames: ["org-docs"],
                Resources: null,
                BootstrapMessage: null),
            memberId,
            opsWorkspace.Id);

        var assignedIntegrations = await new AgentIntegrationRepository(db).ListIntegrationNamesForAgentAsync(agent.Id, CancellationToken.None);
        var orgAgents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = opsWorkspace.Id });

        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal(WorkspaceRole.Editor, memberOrgDefault.Role);
        Assert.Equal(WorkspaceRole.Editor, seededOpsWorkspace.Role);
        Assert.Equal(WorkspaceRole.Viewer, opsMembership.Role);
        Assert.Equal(WorkspaceRole.Editor, upgradedMembership.Role);
        Assert.NotNull(memberVisibleIntegration);
        Assert.True(memberVisibleIntegration.CredentialConfigured);
        Assert.Null(personalIntegration);
        Assert.Contains("org-docs", assignedIntegrations);
        Assert.Single(orgAgents);
        Assert.Equal(memberId, orgAgents[0].OwnerId);
    }

    [Fact]
    public async Task Pending_invites_are_visible_after_account_creation_and_require_acceptance()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var ownerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var invite = await harness.Organizations.InviteMemberAsync(
            ownerId,
            "owner@example.com",
            "Owner",
            "member@example.com",
            "Editor");

        await WorkspaceTestHarness.SeedUserAsync(db, invitedUserId, "member@example.com");

        var pending = await harness.Organizations.ListPendingInvitesAsync(invitedUserId, "member@example.com");
        var workspacesBeforeAccept = await harness.Workspaces.ListAsync(invitedUserId);
        var accepted = await harness.Organizations.AcceptInviteAsync(invitedUserId, "member@example.com", invite.Id);
        var workspacesAfterAccept = await harness.Workspaces.ListAsync(invitedUserId);

        Assert.Single(pending);
        Assert.Equal(invite.Id, pending[0].Id);
        Assert.DoesNotContain(workspacesBeforeAccept, workspace => workspace.OrganizationId == overview.Organization.Id);
        Assert.Equal(MemberStatus.Active, accepted.Status);
        Assert.Equal(invitedUserId, accepted.UserId);
        Assert.Contains(workspacesAfterAccept, workspace => workspace.OrganizationId == overview.Organization.Id);
    }

    [Fact]
    public async Task Access_groups_grant_workspace_access_without_direct_workspace_membership()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        await WorkspaceTestHarness.SeedUserAsync(db, memberId, "member@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var workspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Finance");
        var invite = await harness.Organizations.InviteMemberAsync(ownerId, "owner@example.com", "Owner", "member@example.com", "Editor");
        await harness.Organizations.AcceptInviteAsync(memberId, "member@example.com", invite.Id);
        var group = await harness.AccessGroups.CreateAsync(ownerId, overview.Organization.Id, "Finance");

        await harness.AccessGroups.AddMemberAsync(ownerId, group.Id, memberId);
        await harness.AccessGroups.GrantWorkspaceAsync(ownerId, group.Id, workspace.Id, "Viewer");

        var workspaces = await harness.Workspaces.ListAsync(memberId);
        var accessible = Assert.Single(workspaces, item => item.Id == workspace.Id);

        Assert.Equal(WorkspaceRole.Viewer, accessible.Role);
    }

    [Fact]
    public async Task Organization_members_cannot_change_org_admin_configuration()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        await WorkspaceTestHarness.SeedUserAsync(db, memberId, "member@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var invite = await harness.Organizations.InviteMemberAsync(ownerId, "owner@example.com", "Owner", "member@example.com", "Editor");
        await harness.Organizations.AcceptInviteAsync(memberId, "member@example.com", invite.Id);
        var workspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Ops");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ProviderProfiles.SaveAsync(
                memberId,
                overview.Organization.Id,
                "openai",
                "OpenAI",
                ["gpt-4o-mini"],
                "key",
                true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Policy.UpdateAsync(
                memberId,
                new OrganizationPolicyProfileRecord
                {
                    OrganizationId = overview.Organization.Id,
                    ShellToolsEnabled = false,
                }));
        await harness.IntegrationDeployments.DeployAsync(memberId, overview.Organization.Id, workspace.Id, "org-docs");
        await harness.Integrations.RegisterAsync(memberId, workspace.Id, WorkspaceTestHarness.CustomIntegration());
        await harness.Integrations.SaveCredentialAsync(memberId, workspace.Id, "org-docs", new() { ["API_KEY"] = "secret" });
    }

    [Fact]
    public async Task Organization_workspace_custom_integrations_require_deployment_for_visibility_and_assignment()
    {
        await using var db = WorkspaceTestHarness.CreateDb();
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");

        var harness = WorkspaceTestHarness.Create(db);
        var overview = await harness.Organizations.GetOverviewAsync(ownerId, "owner@example.com", "Owner");
        var workspace = await harness.Workspaces.CreateOrganizationWorkspaceAsync(ownerId, overview.Organization.Id, "Ops");
        db.Integrations.Add(new IntegrationDefinitionEntity
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            WorkspaceId = workspace.Id,
            Name = "org-docs",
            Provider = "",
            Title = "Org Docs",
            TransportType = IntegrationTransportType.Stdio.ToString(),
            Command = "npx",
            Args = """["-y","org-docs"]""",
            Category = "custom",
            IsBuiltin = false,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var hidden = await harness.Integrations.ListAsync(ownerId, workspace.Id);
        var agent = await harness.AgentDashboard.CreateAsync(
            new CreateDashboardAgentRequest(
                Name: "Org Agent",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: null,
                IntegrationSlugs: null,
                ChannelConnectionIds: null,
                ToolNames: null,
                Resources: null,
                BootstrapMessage: null),
            ownerId,
            workspace.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Integrations.AssignToAgentAsync(agent.Id, "org-docs", ownerId));

        await harness.IntegrationDeployments.DeployAsync(ownerId, overview.Organization.Id, workspace.Id, "org-docs");
        var visible = await harness.Integrations.ListAsync(ownerId, workspace.Id);
        await harness.Integrations.AssignToAgentAsync(agent.Id, "org-docs", ownerId);
        var assigned = await new AgentIntegrationRepository(db).ListIntegrationNamesForAgentAsync(agent.Id, CancellationToken.None);

        Assert.DoesNotContain(hidden, integration => integration.Name == "org-docs");
        Assert.Contains(visible, integration => integration.Name == "org-docs");
        Assert.Contains("org-docs", assigned);
    }

}

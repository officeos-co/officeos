using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class WorkspaceRepositoryTests
{
    [Fact]
    public async Task ListAccessibleAsync_returns_workspace_role_bindings()
    {
        var userId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("workspaces");
        db.Users.Add(new UserEntity { Id = userId, Email = "member@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);
        var workspace = await repository.EnsurePersonalDefaultAsync(userId);

        var rows = await repository.ListAccessibleAsync(userId);

        Assert.Contains(rows, row => row.Id == workspace.Id && row.OwnerKind == WorkspaceOwnerKind.Personal && row.Role == WorkspaceRole.Owner);
    }

    [Fact]
    public async Task GetCurrentAsync_ignores_current_workspace_when_user_lacks_membership()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("workspaces");
        db.Users.Add(new UserEntity { Id = ownerId, Email = "owner@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = outsiderId, Email = "outsider@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);
        var ownerWorkspace = await repository.EnsurePersonalDefaultAsync(ownerId);
        var outsider = await db.Users.FirstAsync(u => u.Id == outsiderId);
        outsider.CurrentWorkspaceId = ownerWorkspace.Id;
        await db.SaveChangesAsync();

        var current = await repository.GetCurrentAsync(outsiderId);

        Assert.Equal(WorkspaceOwnerKind.Personal, current.OwnerKind);
        Assert.Equal(outsiderId, current.OwnerUserId);
        Assert.NotEqual(ownerWorkspace.Id, current.Id);
    }

    [Fact]
    public async Task GetCurrentAsync_ignores_legacy_non_personal_current_workspace()
    {
        var userId = Guid.NewGuid();
        var legacyWorkspaceId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("workspaces");
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = "owner@example.com",
            CurrentWorkspaceId = legacyWorkspaceId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = legacyWorkspaceId,
            OwnerKind = "organization",
            OwnerUserId = null,
            Name = "Legacy Org",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.WorkspaceMembers.Add(new WorkspaceMemberEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = legacyWorkspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);

        var current = await repository.GetCurrentAsync(userId);

        Assert.Equal(WorkspaceOwnerKind.Personal, current.OwnerKind);
        Assert.Equal(userId, current.OwnerUserId);
        Assert.NotEqual(legacyWorkspaceId, current.Id);
    }

    [Fact]
    public async Task ListAccessibleAsync_skips_legacy_non_personal_workspaces()
    {
        var userId = Guid.NewGuid();
        var legacyWorkspaceId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("workspaces");
        db.Users.Add(new UserEntity { Id = userId, Email = "owner@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = legacyWorkspaceId,
            OwnerKind = "organization",
            OwnerUserId = null,
            Name = "Legacy Org",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.WorkspaceMembers.Add(new WorkspaceMemberEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = legacyWorkspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);
        var personal = await repository.EnsurePersonalDefaultAsync(userId);

        var rows = await repository.ListAccessibleAsync(userId);

        Assert.Contains(rows, row => row.Id == personal.Id);
        Assert.DoesNotContain(rows, row => row.Id == legacyWorkspaceId);
    }
}

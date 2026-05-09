using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Features.Management;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class WorkspaceRepositoryTests
{
    [Fact]
    public async Task ListAccessibleAsync_returns_personal_and_org_workspaces_without_merging_them()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await using var db = CreateDb();
        db.Users.Add(new UserEntity { Id = userId, Email = "member@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = userId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);
        var personal = await repository.EnsurePersonalDefaultAsync(userId);
        var organization = await repository.EnsureOrganizationDefaultAsync(organizationId, userId);

        var rows = await repository.ListAccessibleAsync(userId);

        Assert.Contains(rows, row => row.Id == personal.Id && row.OwnerKind == WorkspaceOwnerKind.Personal && row.Role == WorkspaceRole.Owner);
        Assert.Contains(rows, row => row.Id == organization.Id && row.OwnerKind == WorkspaceOwnerKind.Organization && row.Role == WorkspaceRole.Owner);
    }

    [Fact]
    public async Task GetCurrentAsync_ignores_current_workspace_when_user_lacks_membership()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        await using var db = CreateDb();
        db.Users.Add(new UserEntity { Id = ownerId, Email = "owner@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = outsiderId, Email = "outsider@example.com", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = ownerId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var repository = new WorkspaceRepository(db);
        var organization = await repository.EnsureOrganizationDefaultAsync(organizationId, ownerId);
        var outsider = await db.Users.FirstAsync(u => u.Id == outsiderId);
        outsider.CurrentWorkspaceId = organization.Id;
        await db.SaveChangesAsync();

        var current = await repository.GetCurrentAsync(outsiderId);

        Assert.Equal(WorkspaceOwnerKind.Personal, current.OwnerKind);
        Assert.Equal(outsiderId, current.OwnerUserId);
        Assert.NotEqual(organization.Id, current.Id);
    }

    private static EaosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"workspaces-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }
}

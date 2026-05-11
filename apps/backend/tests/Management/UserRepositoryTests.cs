using OffceOs.Database.Models;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class UserRepositoryTests
{
    [Fact]
    public async Task UpsertByGitHubSubjectAsync_links_existing_email_user()
    {
        var existingUserId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("users");
        db.Users.Add(new UserEntity
        {
            Id = existingUserId,
            Email = "owner@example.com",
            GoogleSubjectId = "google-subject",
            Name = "Owner",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastLoginAt = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var repository = new UserRepository(db);

        var user = await repository.UpsertByGitHubSubjectAsync(
            "github-subject",
            "OWNER@example.com",
            "Owner From GitHub",
            "https://example.com/avatar.png",
            CancellationToken.None);

        Assert.Equal(existingUserId, user.Id);
        Assert.Equal("owner@example.com", user.Email);
        Assert.Equal("google-subject", user.GoogleSubjectId);
        Assert.Equal("github-subject", user.GitHubSubjectId);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task UpsertByGoogleSubjectAsync_links_existing_email_user()
    {
        var existingUserId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("users");
        db.Users.Add(new UserEntity
        {
            Id = existingUserId,
            Email = "owner@example.com",
            GitHubSubjectId = "github-subject",
            Name = "Owner",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastLoginAt = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var repository = new UserRepository(db);

        var user = await repository.UpsertByGoogleSubjectAsync(
            "google-subject",
            "OWNER@example.com",
            "Owner From Google",
            "https://example.com/avatar.png",
            CancellationToken.None);

        Assert.Equal(existingUserId, user.Id);
        Assert.Equal("owner@example.com", user.Email);
        Assert.Equal("github-subject", user.GitHubSubjectId);
        Assert.Equal("google-subject", user.GoogleSubjectId);
        Assert.Equal(1, await db.Users.CountAsync());
    }
}

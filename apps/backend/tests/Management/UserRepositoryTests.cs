using OffceOs.Database.Models;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;

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
    public async Task UpsertByGitHubSubjectAsync_moves_subject_to_existing_email_user()
    {
        var staleSubjectUserId = Guid.NewGuid();
        var emailUserId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("users");
        db.Users.Add(new UserEntity
        {
            Id = staleSubjectUserId,
            Email = "old@example.com",
            GitHubSubjectId = "github-subject",
            Name = "Old Owner",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastLoginAt = DateTime.UtcNow.AddDays(-2),
        });
        db.Users.Add(new UserEntity
        {
            Id = emailUserId,
            Email = "owner@example.com",
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

        Assert.Equal(emailUserId, user.Id);
        Assert.Equal("owner@example.com", user.Email);
        Assert.Equal("github-subject", user.GitHubSubjectId);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Null((await db.Users.FindAsync(new object?[] { staleSubjectUserId }, CancellationToken.None))!.GitHubSubjectId);
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

    [Fact]
    public async Task UpsertByGoogleSubjectAsync_moves_subject_to_existing_email_user()
    {
        var staleSubjectUserId = Guid.NewGuid();
        var emailUserId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("users");
        db.Users.Add(new UserEntity
        {
            Id = staleSubjectUserId,
            Email = "old@example.com",
            GoogleSubjectId = "google-subject",
            Name = "Old Owner",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastLoginAt = DateTime.UtcNow.AddDays(-2),
        });
        db.Users.Add(new UserEntity
        {
            Id = emailUserId,
            Email = "owner@example.com",
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

        Assert.Equal(emailUserId, user.Id);
        Assert.Equal("owner@example.com", user.Email);
        Assert.Equal("google-subject", user.GoogleSubjectId);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Null((await db.Users.FindAsync(new object?[] { staleSubjectUserId }, CancellationToken.None))!.GoogleSubjectId);
    }
}

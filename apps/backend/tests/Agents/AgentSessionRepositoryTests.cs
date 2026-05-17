using OffceOs.Database.Models;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.Agents.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Agents;

public sealed class AgentSessionRepositoryTests
{
    [Fact]
    public async Task Session_runtime_repository_and_pull_request_are_persisted_as_child_records()
    {
        await using var db = TestDbFactory.Create("agent-session-children");
        var repository = new AgentSessionRepository(db);
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agent = AgentRecord.Create("Coder", "openai", "gpt-4o-mini", ownerId, workspaceId: workspaceId);
        db.Agents.Add(new AgentEntity
        {
            Id = agent.Id,
            Name = agent.Name,
            Provider = agent.Provider,
            Model = agent.Model,
            Status = AgentStatus.Idle.ToStorageString(),
            CreatedAt = agent.CreatedAt,
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
        });
        await db.SaveChangesAsync();

        var session = AgentSessionRecord.CreateRun(
            agent,
            "fix the failing test",
            AgentWorkPurposeKinds.Routine,
            AgentSessionSourceKinds.GitHub,
            "corr-1",
            repository: new AgentSessionRepositoryConfig(
                "acme/widget",
                "https://github.com/acme/widget.git",
                "main",
                "github"));
        await repository.CreateAsync(session);

        session.MarkRunning("sandbox-1", "http://sandbox", DateTime.UtcNow);
        session.RecordGitHubArtifact("officeos/session-123", "abc123", "https://github.com/acme/widget/pull/5", 5);
        await repository.SaveAsync(session);

        var saved = await repository.GetByAsync(new AgentSessionFilter { Id = session.Id });

        Assert.NotNull(saved);
        Assert.Equal("sandbox-1", saved.Runtime?.SandboxId);
        Assert.Equal("acme/widget", saved.Repository?.FullName);
        Assert.Equal("officeos/session-123", saved.Repository?.Branch);
        Assert.Equal("https://github.com/acme/widget/pull/5", saved.PullRequest?.Url);
        Assert.Equal("abc123", saved.PullRequest?.CommitSha);
    }
}

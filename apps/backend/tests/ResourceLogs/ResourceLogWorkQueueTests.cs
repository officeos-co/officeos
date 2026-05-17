using OffceOs.Database;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Features.ResourceLogs.Infrastructure;

namespace OffceOs.Tests.ResourceLogs;

public sealed class ResourceLogWorkQueueTests
{
    [Fact]
    public async Task ClaimNextQueuedWorkAsync_claims_oldest_queued_work()
    {
        using var db = CreateDb();
        var repository = new ResourceLogRepository(db);
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var newer = Work(agentId, workspaceId, DateTime.UtcNow);
        var older = Work(agentId, workspaceId, DateTime.UtcNow.AddMinutes(-1));
        await repository.UpsertQueuedWorkAsync(newer);
        await repository.UpsertQueuedWorkAsync(older);

        var claimed = await repository.ClaimNextQueuedWorkAsync();

        Assert.NotNull(claimed);
        Assert.Equal(older.Id, claimed.Id);
        Assert.Equal(AgentWorkStatusKinds.Running, claimed.WorkStatus);
    }

    [Fact]
    public async Task ClaimNextQueuedWorkAsync_skips_agent_that_already_has_running_work()
    {
        using var db = CreateDb();
        var repository = new ResourceLogRepository(db);
        var busyAgentId = Guid.NewGuid();
        var busySessionId = Guid.NewGuid();
        var freeAgentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        await repository.AppendAsync(Work(busyAgentId, workspaceId, DateTime.UtcNow.AddMinutes(-2), AgentWorkStatusKinds.Running, busySessionId));
        await repository.UpsertQueuedWorkAsync(Work(busyAgentId, workspaceId, DateTime.UtcNow.AddMinutes(-1), sessionId: busySessionId));
        var freeWork = Work(freeAgentId, workspaceId, DateTime.UtcNow);
        await repository.UpsertQueuedWorkAsync(freeWork);

        var claimed = await repository.ClaimNextQueuedWorkAsync();

        Assert.NotNull(claimed);
        Assert.Equal(freeWork.Id, claimed.Id);
    }

    [Fact]
    public async Task MarkWorkAsync_records_completion_state_on_work_log()
    {
        using var db = CreateDb();
        var repository = new ResourceLogRepository(db);
        var work = Work(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await repository.UpsertQueuedWorkAsync(work);

        await repository.MarkWorkAsync(work.Id, AgentWorkStatusKinds.Completed);

        var saved = await repository.GetByAsync(new ResourceLogFilter { Id = work.Id });
        Assert.NotNull(saved);
        Assert.Equal(AgentWorkStatusKinds.Completed, saved.WorkStatus);
        Assert.NotNull(saved.CompletedAt);
    }

    private static ResourceLogRecord Work(
        Guid agentId,
        Guid workspaceId,
        DateTime time,
        string status = AgentWorkStatusKinds.Queued,
        Guid? sessionId = null) => new()
    {
        Id = Guid.NewGuid(),
        AgentId = agentId,
        SessionId = sessionId ?? Guid.NewGuid(),
        WorkspaceId = workspaceId,
        ResourceKind = ResourceLogKinds.Agent,
        ResourceId = agentId,
        Type = ResourceLogType.MessageIn,
        Content = "Do work.",
        CorrelationId = Guid.NewGuid().ToString("N"),
        Time = time,
        WorkStatus = status,
        WorkPurpose = AgentWorkPurposeKinds.Manual,
    };

    private static EaosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"agent-log-work-queue-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }
}

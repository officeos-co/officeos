using System.Text;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Analytics;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Analytics;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.AgentRoutines;
using OffceOs.Tests.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.AgentRoutines;

public sealed class AgentRoutineEndToEndTests
{
    [Fact]
    public async Task Routine_can_store_schedule_api_and_github_triggers_for_one_agent()
    {
        await using var db = CreateDb();
        var (ownerId, workspaceId, agentId) = await SeedAgentAsync(db);
        var (service, _, _) = CreateServices(db);

        var result = await service.CreateAsync(
            new CreateAgentRoutineRequest(
                agentId,
                "Review automation",
                "Review the incoming engineering event.",
                [new CreateScheduleRoutineTriggerRequest("Weekday check", "*/5 * * * *")],
                [new CreateApiRoutineTriggerRequest("Manual deployment hook")],
                [
                    new CreateGitHubRoutineTriggerRequest("Pull requests", "acme", "platform", ["pull_request"], "github-secret"),
                    new CreateGitHubRoutineTriggerRequest("Pushes", "acme", "platform", ["push"], "github-secret")
                ]),
            ownerId,
            workspaceId);

        var routines = await service.ListForOwnerAsync(ownerId, workspaceId);
        var routine = Assert.Single(routines);

        Assert.Equal(result.Routine.Id, routine.Routine.Id);
        Assert.Equal("Review automation", routine.Routine.Name);
        Assert.Equal("agent-one", routine.AgentName);
        Assert.Equal(4, routine.Routine.Triggers.Count);
        Assert.Contains(routine.Routine.Triggers, trigger => trigger.Kind == AgentRoutineTriggerKinds.Schedule);
        Assert.Contains(routine.Routine.Triggers, trigger => trigger.Kind == AgentRoutineTriggerKinds.Api);
        Assert.Equal(2, routine.Routine.Triggers.Count(trigger => trigger.Kind == AgentRoutineTriggerKinds.GitHub));
        Assert.Single(result.GeneratedSecrets);
        Assert.Equal(AgentRoutineTriggerKinds.Api, result.GeneratedSecrets[0].Kind);
        Assert.False(string.IsNullOrWhiteSpace(result.GeneratedSecrets[0].Secret));
    }

    [Fact]
    public async Task Due_schedule_trigger_sends_routine_prompt_and_advances_trigger_state()
    {
        await using var db = CreateDb();
        var (ownerId, workspaceId, agentId) = await SeedAgentAsync(db);
        var (service, execution, logs) = CreateServices(db);
        var create = await service.CreateAsync(
            new CreateAgentRoutineRequest(
                agentId,
                "Daily triage",
                "Summarize outstanding pull requests.",
                [new CreateScheduleRoutineTriggerRequest("Every minute", "* * * * *")],
                [],
                []),
            ownerId,
            workspaceId);
        var trigger = Assert.Single(create.Routine.Triggers);
        trigger.SetNextRun(DateTime.UtcNow.AddMinutes(-1));
        await new AgentRoutineRepository(db).UpsertAsync(create.Routine);

        var fired = await execution.RunDueSchedulesAsync(DateTime.UtcNow);

        Assert.Equal(1, fired.TriggeredCount);
        var sent = Assert.Single(logs.Messages);
        Assert.Equal(agentId, sent.AgentId);
        Assert.Contains("[Routine: Daily triage]", sent.Content);
        Assert.Contains("[Trigger: Every minute]", sent.Content);
        Assert.Contains("Summarize outstanding pull requests.", sent.Content);

        var saved = await new AgentRoutineRepository(db).GetByAsync(new AgentRoutineFilter { Id = create.Routine.Id });
        var savedTrigger = Assert.Single(saved!.Triggers);
        Assert.NotNull(savedTrigger.LastTriggeredAt);
        Assert.NotNull(savedTrigger.NextRunAt);
        Assert.True(savedTrigger.NextRunAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Api_trigger_requires_generated_secret_and_sends_payload_context()
    {
        await using var db = CreateDb();
        var (ownerId, workspaceId, agentId) = await SeedAgentAsync(db);
        var (service, execution, logs) = CreateServices(db);
        var create = await service.CreateAsync(
            new CreateAgentRoutineRequest(
                agentId,
                "Release workflow",
                "Run release validation.",
                [],
                [new CreateApiRoutineTriggerRequest("Deploy hook")],
                []),
            ownerId,
            workspaceId);
        var trigger = Assert.Single(create.Routine.Triggers);
        var secret = Assert.Single(create.GeneratedSecrets).Secret;

        var rejected = await execution.ExecuteApiTriggerAsync(trigger.Id, "wrong-secret", """{"environment":"prod"}""");
        var accepted = await execution.ExecuteApiTriggerAsync(trigger.Id, secret, """{"environment":"prod"}""");

        Assert.Equal(0, rejected.TriggeredCount);
        Assert.Equal(1, accepted.TriggeredCount);
        var sent = Assert.Single(logs.Messages);
        Assert.Equal(agentId, sent.AgentId);
        Assert.Contains("[Trigger type: api]", sent.Content);
        Assert.Contains("Run release validation.", sent.Content);
        Assert.Contains("\"environment\":\"prod\"", sent.Content);
    }

    [Fact]
    public async Task Github_webhook_verifies_signature_matches_repo_and_event_then_sends_prompt()
    {
        await using var db = CreateDb();
        var (ownerId, workspaceId, agentId) = await SeedAgentAsync(db);
        var (service, execution, logs) = CreateServices(db);
        await service.CreateAsync(
            new CreateAgentRoutineRequest(
                agentId,
                "PR watcher",
                "Review the pull request update.",
                [],
                [],
                [new CreateGitHubRoutineTriggerRequest("PR events", "acme", "platform", ["pull_request"], "github-secret")]),
            ownerId,
            workspaceId);

        const string payload = """{"repository":{"full_name":"acme/platform"},"pull_request":{"number":42,"title":"Add routines"},"action":"opened"}""";
        var invalid = await execution.ExecuteGitHubWebhookAsync(new GitHubRoutineWebhookRequest("pull_request", "sha256=invalid", payload));
        var valid = await execution.ExecuteGitHubWebhookAsync(new GitHubRoutineWebhookRequest("pull_request", Sign(payload, "github-secret"), payload));

        Assert.Equal(0, invalid.TriggeredCount);
        Assert.Equal(1, valid.TriggeredCount);
        var sent = Assert.Single(logs.Messages);
        Assert.Equal(agentId, sent.AgentId);
        Assert.Contains("[Trigger type: github]", sent.Content);
        Assert.Contains("Review the pull request update.", sent.Content);
        Assert.Contains("\"full_name\":\"acme/platform\"", sent.Content);
    }

    private static (IAgentRoutineService Service, AgentRoutineExecutionService Execution, RecordingAgentLogService Logs) CreateServices(EaosDbContext db)
    {
        var agentRepository = new AgentRepository(db);
        var routineRepository = new AgentRoutineRepository(db);
        var logs = new RecordingAgentLogService();
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-routine-test-keys-{Guid.NewGuid():N}");
        var credentialProtector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));
        var service = new AgentRoutineService(routineRepository, agentRepository, credentialProtector);
        var execution = new AgentRoutineExecutionService(routineRepository, logs, credentialProtector, NullLogger<AgentRoutineExecutionService>.Instance);
        return (service, execution, logs);
    }

    private static async Task<(Guid OwnerId, Guid WorkspaceId, Guid AgentId)> SeedAgentAsync(EaosDbContext db)
    {
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        db.Users.Add(new UserEntity
        {
            Id = ownerId,
            Email = "owner@example.com",
            Name = "Owner",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OwnerUserId = ownerId,
            OwnerKind = WorkspaceOwnerKind.Personal.ToStorageString(),
            Name = "Personal",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "agent-one",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = AgentStatus.Running.ToStorageString(),
            PodName = "agent-pod",
            OwnerId = ownerId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (ownerId, workspaceId, agentId);
    }

    private static EaosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"agent-routines-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }

    private static string Sign(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed class RecordingAgentLogService : IAgentLogService
    {
        public List<(Guid AgentId, string Content, Guid UserId)> Messages { get; } = [];

        public IQueryable<AgentLogProjection> AgentLogs(Guid agentId, Guid? workspaceId = null) =>
            Enumerable.Empty<AgentLogProjection>().AsQueryable();

        public IQueryable<AgentLogProjection> ChannelLogs(Guid channelConnectionId, Guid? workspaceId = null) =>
            Enumerable.Empty<AgentLogProjection>().AsQueryable();

        public IQueryable<AgentLogProjection> GlobalLogs(GlobalLogFiltersRequest filters, Guid? workspaceId = null) =>
            Enumerable.Empty<AgentLogProjection>().AsQueryable();

        public IQueryable<AuditEntry> AuditLog(Guid agentId, Guid? workspaceId = null) =>
            Enumerable.Empty<AuditEntry>().AsQueryable();

        public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default) =>
            Task.FromResult(new List<AgentLogRecord>());

        public Task<List<AgentLogRecord>> ListForChannelConnectionAsync(Guid channelConnectionId, DateTime? before, int limit, CancellationToken ct = default) =>
            Task.FromResult(new List<AgentLogRecord>());

        public Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersRequest filters, CancellationToken ct = default) =>
            Task.FromResult(new GlobalLogsPage([], 0));

        public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default) =>
            Task.FromResult(record);

        public Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
        {
            Messages.Add((agentId, content, userId));
            return Task.FromResult(AgentLogRecord.MessageIn(agentId, content));
        }

        public Task RecordToolCallAsync(Guid agentId, Guid? userId, string skillName, string action, string paramsJson, string? resultSummary, long durationMs, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset, CancellationToken ct = default) =>
            Task.FromResult((new List<AgentLogRecord>(), 0));

        public Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default) =>
            Task.FromResult(new Dictionary<string, AgentLogRecord>());
    }
}

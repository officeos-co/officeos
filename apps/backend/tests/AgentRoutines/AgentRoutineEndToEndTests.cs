using System.Text;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Observability;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Observability;
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

    private static (IAgentRoutineService Service, AgentRoutineExecutionService Execution, RecordingAgentService Logs) CreateServices(EaosDbContext db)
    {
        var agentRepository = new AgentRepository(db);
        var routineRepository = new AgentRoutineRepository(db);
        var logs = new RecordingAgentLogService();
        var agents = new RecordingAgentService();
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-routine-test-keys-{Guid.NewGuid():N}");
        var credentialProtector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));
        var service = new AgentRoutineService(routineRepository, agentRepository, credentialProtector);
        var execution = new AgentRoutineExecutionService(routineRepository, logs, agents, credentialProtector, NullLogger<AgentRoutineExecutionService>.Instance);
        return (service, execution, agents);
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
            Status = AgentStatus.Idle.ToStorageString(),
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
        public Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default) =>
            Task.FromResult(new AgentLogPage([], 0));

        public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default)
        {
            var ids = (request.AgentIds ?? []).Concat(request.ChannelConnectionIds ?? []).Distinct();
            return Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
                ids.ToDictionary(id => id, _ => (string?)null));
        }

        public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default) =>
            Task.FromResult(record);
    }

    private sealed class RecordingAgentService : IAgentService
    {
        public List<(Guid AgentId, string Content, Guid UserId, string? Purpose)> Messages { get; } = [];

        public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentRecord>>([]);

        public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default) =>
            Task.FromResult<AgentRecord?>(null);

        public Task<AgentRecord> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentRecord?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default) =>
            Task.FromResult<AgentRecord?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<AgentLogRecord> SendMessageAsync(
            Guid agentId,
            string content,
            Guid userId,
            CancellationToken ct = default,
            string? runPurpose = null,
            Guid? definitionId = null)
        {
            Messages.Add((agentId, content, userId, runPurpose));
            return Task.FromResult(AgentLogRecord.MessageIn(agentId, content));
        }
    }
}

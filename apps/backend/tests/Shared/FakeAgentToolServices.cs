using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Context;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Context;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;

namespace OffceOs.Tests.Shared;

public sealed class FakeOrganizationPolicyService : IOrganizationPolicyService
{
    private readonly OrganizationPolicyProfileRecord? _policy;

    public FakeOrganizationPolicyService(OrganizationPolicyProfileRecord? policy = null) => _policy = policy;

    public Task<OrganizationPolicyProfileRecord?> GetEffectiveForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult(_policy);

    public Task<OrganizationPolicyProfileRecord> GetOrCreateAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default) =>
        Task.FromResult(_policy ?? OrganizationPolicyProfileRecord.Default(organizationId));

    public Task<OrganizationPolicyProfileRecord> UpdateAsync(Guid actorUserId, OrganizationPolicyProfileRecord profile, CancellationToken ct = default) =>
        Task.FromResult(profile);
}

public sealed class FakeAgentMemoryService : IAgentMemoryService
{
    public Task StoreAsync(Guid agentId, string key, string content, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<AgentMemoryRecord>> RecallAsync(Guid agentId, string query, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentMemoryRecord>>([]);

    public Task<bool> ForgetAsync(Guid agentId, string key, CancellationToken ct = default) => Task.FromResult(false);
}

public sealed class FakeAgentRoutineRepository : IAgentRoutineRepository
{
    public Task<IReadOnlyList<AgentRoutineRecord>> ListAsync(AgentRoutineFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRoutineRecord>>([]);

    public Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRoutineWithAgentRecord>>([]);

    public Task<IReadOnlyList<AgentRoutineRecord>> ListAllEnabledAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRoutineRecord>>([]);

    public Task<AgentRoutineRecord?> GetByAsync(AgentRoutineFilter filter, CancellationToken ct = default) =>
        Task.FromResult<AgentRoutineRecord?>(null);

    public Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<AgentRoutineWithAgentRecord?>(null);

    public Task<AgentRoutineTriggerRecord?> GetTriggerByAsync(Guid triggerId, CancellationToken ct = default) =>
        Task.FromResult<AgentRoutineTriggerRecord?>(null);

    public Task<AgentRoutineRecord> UpsertAsync(AgentRoutineRecord record, CancellationToken ct = default) =>
        Task.FromResult(record);

    public Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
}

public sealed class FakeAgentRunRepository : IAgentRunRepository
{
    public Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.FromResult(run);
    public Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default) => Task.FromResult<AgentRunRecord?>(null);
    public Task<IReadOnlyList<AgentRunRecord>> ListAsync(AgentRunFilter filter, int limit = 100, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRunRecord>>([]);

    public Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRunRecord>>([]);

    public Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class FakeAgentRoutineService : IAgentRoutineService
{
    public CreateAgentRoutineRequest? LastCreateRequest { get; private set; }
    public Guid? LastOwnerId { get; private set; }
    public Guid? LastWorkspaceId { get; private set; }

    public Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRoutineWithAgentRecord>>([]);

    public Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult<AgentRoutineWithAgentRecord?>(null);

    public Task<IReadOnlyList<AgentRoutineRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRoutineRecord>>([]);

    public Task<AgentRoutineCreateResult> CreateAsync(CreateAgentRoutineRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        LastCreateRequest = request;
        LastOwnerId = ownerId;
        LastWorkspaceId = workspaceId;

        var routine = AgentRoutineRecord.Create(request.AgentId, request.Name, request.Prompt);
        foreach (var trigger in request.ScheduleTriggers)
        {
            var cron = Cronos.CronExpression.Parse(trigger.Expression);
            routine.Triggers.Add(AgentRoutineTriggerRecord.CreateSchedule(routine.Id, trigger.Name, trigger.Expression, cron.GetNextOccurrence(DateTime.UtcNow, inclusive: false)));
        }

        var secrets = new List<AgentRoutineGeneratedSecretResult>();
        foreach (var trigger in request.ApiTriggers)
        {
            var apiTrigger = AgentRoutineTriggerRecord.CreateApi(routine.Id, trigger.Name, "hashed-secret");
            routine.Triggers.Add(apiTrigger);
            secrets.Add(new AgentRoutineGeneratedSecretResult(apiTrigger.Id, apiTrigger.Kind, apiTrigger.Name, "generated-secret"));
        }

        foreach (var trigger in request.GitHubTriggers)
        {
            routine.Triggers.Add(AgentRoutineTriggerRecord.CreateGitHub(
                routine.Id,
                trigger.Name,
                trigger.Owner,
                trigger.Repo,
                trigger.Events,
                "encrypted-secret"));
        }

        return Task.FromResult(new AgentRoutineCreateResult(routine, secrets));
    }

    public Task<bool> SetEnabledAsync(Guid id, Guid ownerId, Guid workspaceId, bool enabled, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult(true);
}

internal sealed class NoBrowserToolContextFactory : IBrowserToolContextFactory
{
    public Task<BrowserToolContext?> CreateForTurnAsync(CancellationToken ct = default) =>
        Task.FromResult<BrowserToolContext?>(null);

    public Task<BrowserToolContext> CreateCatalogAsync(CancellationToken ct = default) =>
        throw new NotSupportedException();
}

public sealed class EmptyAgentDefinitionRepository : IAgentDefinitionRepository
{
    public Task<AgentDefinitionRecord?> GetByAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
        Task.FromResult<AgentDefinitionRecord?>(null);

    public Task<IReadOnlyList<AgentDefinitionRecord>> ListAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentDefinitionRecord>>([]);

    public Task AddAsync(AgentDefinitionRecord definition, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<int> GetNextVersionAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult(1);
}

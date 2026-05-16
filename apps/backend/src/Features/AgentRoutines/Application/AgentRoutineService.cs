using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Agents;
using OffceOs.Infrastructure.Common.Security;
namespace OffceOs.Application.Features.AgentRoutines;

internal sealed class AgentRoutineService : IAgentRoutineService
{
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly CredentialProtector _credentialProtector;

    public AgentRoutineService(
        IAgentRoutineRepository agentRoutineRepository,
        IAgentRepository agentRepository,
        CredentialProtector credentialProtector)
    {
        _agentRoutineRepository = agentRoutineRepository;
        _agentRepository = agentRepository;
        _credentialProtector = credentialProtector;
    }

    public Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        _agentRoutineRepository.ListForOwnerAsync(null, workspaceId, ct);

    public Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        _agentRoutineRepository.GetForOwnerAsync(id, null, workspaceId, ct);

    public async Task<IReadOnlyList<AgentRoutineRecord>> ListForAgentAsync(Guid agentId, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(agentId, workspaceId, ct);
        return await _agentRoutineRepository.ListAsync(new AgentRoutineFilter { AgentId = agentId }, ct);
    }

    public async Task<AgentRoutineCreateResult> CreateAsync(CreateAgentRoutineRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureAgentOwnedAsync(request.AgentId, workspaceId, ct);

        var routine = AgentRoutineRecord.Create(request.AgentId, request.Name, request.Prompt);
        var generatedSecrets = new List<AgentRoutineGeneratedSecretResult>();
        foreach (var trigger in request.ScheduleTriggers)
            routine.Triggers.Add(AgentRoutineTriggerRecord.CreateSchedule(routine.Id, trigger.Name, trigger.Expression, NextOccurrence(trigger.Expression, DateTime.UtcNow)));

        foreach (var trigger in request.ApiTriggers)
        {
            var secret = GenerateSecret();
            var apiTrigger = AgentRoutineTriggerRecord.CreateApi(routine.Id, trigger.Name, HashSecret(secret));
            routine.Triggers.Add(apiTrigger);
            generatedSecrets.Add(new AgentRoutineGeneratedSecretResult(apiTrigger.Id, apiTrigger.Kind, apiTrigger.Name, secret));
        }

        foreach (var trigger in request.GitHubTriggers)
        {
            if (trigger.Events.Count == 0)
                throw new InvalidOperationException("GitHub routine triggers require at least one event.");

            var mode = GitHubRoutineTriggerModes.Normalize(trigger.Mode);
            if (mode == GitHubRoutineTriggerModes.Poll && string.IsNullOrWhiteSpace(trigger.AuthRef))
                throw new InvalidOperationException("GitHub polling routine triggers require auth_ref.");

            routine.Triggers.Add(AgentRoutineTriggerRecord.CreateGitHub(
                routine.Id,
                trigger.Name,
                trigger.Repo,
                trigger.Events,
                trigger.AuthRef,
                mode,
                TimeSpan.FromSeconds(trigger.PollIntervalSeconds ?? 60),
                null));
        }

        var saved = await _agentRoutineRepository.UpsertAsync(routine, ct);
        return new AgentRoutineCreateResult(saved, generatedSecrets);
    }

    public async Task<bool> SetEnabledAsync(Guid id, Guid ownerId, Guid workspaceId, bool enabled, CancellationToken ct = default)
    {
        var routine = await _agentRoutineRepository.GetForOwnerAsync(id, null, workspaceId, ct);
        if (routine is null) return false;

        await _agentRoutineRepository.SetEnabledAsync(id, enabled, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var routine = await _agentRoutineRepository.GetForOwnerAsync(id, null, workspaceId, ct);
        if (routine is null) return false;

        return await _agentRoutineRepository.DeleteAsync(id, ct);
    }

    private async Task EnsureAgentOwnedAsync(Guid agentId, Guid workspaceId, CancellationToken ct)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspaceId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");
    }

    internal static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static DateTime? NextOccurrence(string expression, DateTime fromUtc)
    {
        var cron = Cronos.CronExpression.Parse(expression);
        return cron.GetNextOccurrence(fromUtc, inclusive: false);
    }
}

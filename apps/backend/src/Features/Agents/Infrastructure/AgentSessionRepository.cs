using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.Agents.Infrastructure;

internal sealed class AgentSessionRepository : IAgentSessionRepository
{
    private readonly EaosDbContext _eaosDbContext;
    private readonly Dictionary<Guid, AgentSessionRecord> _tracked = new();

    public AgentSessionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<AgentSessionRecord?> GetByAsync(AgentSessionFilter filter, CancellationToken ct = default)
    {
        var entity = await ApplyFilter(_eaosDbContext.AgentSessions.AsQueryable(), filter)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (entity is null) return null;
        var record = ToAgentSessionRecord(entity);
        _tracked[record.Id] = record;
        return record;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListAsync(AgentSessionFilter filter, int limit = 100, CancellationToken ct = default)
    {
        var entities = await ApplyFilter(_eaosDbContext.AgentSessions.AsNoTracking(), filter)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return entities.Select(ToAgentSessionRecord).ToList();
    }

    private static IQueryable<AgentSessionEntity> ApplyFilter(IQueryable<AgentSessionEntity> query, AgentSessionFilter filter)
    {
        if (filter.Id.HasValue)
            query = query.Where(s => s.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(s => s.AgentId == filter.AgentId.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(s => s.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(s => s.WorkspaceId == filter.WorkspaceId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            query = query.Where(s => s.CorrelationId == filter.CorrelationId);

        if (filter.RoutineId.HasValue)
            query = query.Where(s => s.RoutineId == filter.RoutineId.Value);

        if (filter.TriggerId.HasValue)
            query = query.Where(s => s.TriggerId == filter.TriggerId.Value);

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value.ToStorageString();
            query = query.Where(s => s.Status == status);
        }

        return query;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListByAgentAsync(Guid agentId, int limit = 20, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return entities.Select(ToAgentSessionRecord).ToList();
    }

    public async Task<AgentSessionRecord> CreateAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        _eaosDbContext.AgentSessions.Add(ToAgentSessionEntity(session));
        await _eaosDbContext.SaveChangesAsync(ct);
        _tracked[session.Id] = session;
        return session;
    }

    public async Task SaveAsync(AgentSessionRecord session, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentSessions.FirstOrDefaultAsync(item => item.Id == session.Id, ct);
        if (entity is null)
        {
            _eaosDbContext.AgentSessions.Add(ToAgentSessionEntity(session));
        }
        else
        {
            MapToAgentSessionEntity(session, entity);
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        _tracked[session.Id] = session;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var record in _tracked.Values)
        {
            var entity = await _eaosDbContext.AgentSessions.FindAsync(new object[] { record.Id }, ct);
            if (entity is null) continue;
            MapToAgentSessionEntity(record, entity);
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default)
        => await _eaosDbContext.AgentSessions.CountAsync(s => s.AgentId == agentId, ct);

    private static AgentSessionRecord ToAgentSessionRecord(AgentSessionEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        OwnerId = e.OwnerId,
        WorkspaceId = e.WorkspaceId,
        Source = e.Source,
        Purpose = e.Purpose,
        CorrelationId = e.CorrelationId,
        RoutineId = e.RoutineId,
        TriggerId = e.TriggerId,
        DefinitionId = e.DefinitionId,
        Input = e.Input,
        TriggerPayloadJson = e.TriggerPayloadJson,
        Status = e.Status.ToSessionStatus(),
        Error = e.Error,
        LastActivityAt = e.LastActivityAt,
        CreatedAt = e.CreatedAt,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
        SandboxId = e.SandboxId,
        ServiceUrl = e.ServiceUrl,
        RepositoryFullName = e.RepositoryFullName,
        RepositoryCloneUrl = e.RepositoryCloneUrl,
        RepositoryBaseBranch = e.RepositoryBaseBranch,
        RepositoryCredentialRef = e.RepositoryCredentialRef,
        RepositoryBranch = e.RepositoryBranch,
        PullRequestUrl = e.PullRequestUrl,
        PullRequestNumber = e.PullRequestNumber,
        CommitSha = e.CommitSha,
        Agent = null,
    };

    private static AgentSessionEntity ToAgentSessionEntity(AgentSessionRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        OwnerId = r.OwnerId,
        WorkspaceId = r.WorkspaceId,
        Source = r.Source,
        Purpose = r.Purpose,
        CorrelationId = r.CorrelationId,
        RoutineId = r.RoutineId,
        TriggerId = r.TriggerId,
        DefinitionId = r.DefinitionId,
        Input = r.Input,
        TriggerPayloadJson = r.TriggerPayloadJson,
        Status = r.Status.ToStorageString(),
        Error = r.Error,
        LastActivityAt = r.LastActivityAt,
        CreatedAt = r.CreatedAt,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        SandboxId = r.SandboxId,
        ServiceUrl = r.ServiceUrl,
        RepositoryFullName = r.RepositoryFullName,
        RepositoryCloneUrl = r.RepositoryCloneUrl,
        RepositoryBaseBranch = r.RepositoryBaseBranch,
        RepositoryCredentialRef = r.RepositoryCredentialRef,
        RepositoryBranch = r.RepositoryBranch,
        PullRequestUrl = r.PullRequestUrl,
        PullRequestNumber = r.PullRequestNumber,
        CommitSha = r.CommitSha,
    };

    private static void MapToAgentSessionEntity(AgentSessionRecord r, AgentSessionEntity e)
    {
        e.OwnerId = r.OwnerId;
        e.WorkspaceId = r.WorkspaceId;
        e.Source = r.Source;
        e.Purpose = r.Purpose;
        e.CorrelationId = r.CorrelationId;
        e.RoutineId = r.RoutineId;
        e.TriggerId = r.TriggerId;
        e.DefinitionId = r.DefinitionId;
        e.Input = r.Input;
        e.TriggerPayloadJson = r.TriggerPayloadJson;
        e.Status = r.Status.ToStorageString();
        e.Error = r.Error;
        e.LastActivityAt = r.LastActivityAt;
        e.StartedAt = r.StartedAt;
        e.CompletedAt = r.CompletedAt;
        e.SandboxId = r.SandboxId;
        e.ServiceUrl = r.ServiceUrl;
        e.RepositoryFullName = r.RepositoryFullName;
        e.RepositoryCloneUrl = r.RepositoryCloneUrl;
        e.RepositoryBaseBranch = r.RepositoryBaseBranch;
        e.RepositoryCredentialRef = r.RepositoryCredentialRef;
        e.RepositoryBranch = r.RepositoryBranch;
        e.PullRequestUrl = r.PullRequestUrl;
        e.PullRequestNumber = r.PullRequestNumber;
        e.CommitSha = r.CommitSha;
    }
}

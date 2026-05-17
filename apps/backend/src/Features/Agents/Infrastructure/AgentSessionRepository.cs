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
        var entity = await ApplyFilter(SessionGraph(_eaosDbContext.AgentSessions), filter)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (entity is null) return null;
        var record = ToAgentSessionRecord(entity);
        _tracked[record.Id] = record;
        return record;
    }

    public async Task<IReadOnlyList<AgentSessionRecord>> ListAsync(AgentSessionFilter filter, int limit = 100, CancellationToken ct = default)
    {
        var entities = await ApplyFilter(SessionGraph(_eaosDbContext.AgentSessions).AsNoTracking(), filter)
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
            .Include(s => s.Runtime)
            .Include(s => s.Repository)
            .Include(s => s.PullRequest)
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
        var entity = await SessionGraph(_eaosDbContext.AgentSessions)
            .FirstOrDefaultAsync(item => item.Id == session.Id, ct);
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
            var entity = await SessionGraph(_eaosDbContext.AgentSessions)
                .FirstOrDefaultAsync(item => item.Id == record.Id, ct);
            if (entity is null) continue;
            MapToAgentSessionEntity(record, entity);
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountByAgentAsync(Guid agentId, CancellationToken ct = default)
        => await _eaosDbContext.AgentSessions.CountAsync(s => s.AgentId == agentId, ct);

    private static IQueryable<AgentSessionEntity> SessionGraph(IQueryable<AgentSessionEntity> query)
        => query
            .Include(s => s.Runtime)
            .Include(s => s.Repository)
            .Include(s => s.PullRequest);

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
        Runtime = ToRuntimeRecord(e.Runtime),
        Repository = ToRepositoryRecord(e.Repository),
        PullRequest = ToPullRequestRecord(e.PullRequest),
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
        Runtime = ToRuntimeEntity(r.Id, r.Runtime),
        Repository = ToRepositoryEntity(r.Id, r.Repository),
        PullRequest = ToPullRequestEntity(r.Id, r.PullRequest),
    };

    private void MapToAgentSessionEntity(AgentSessionRecord r, AgentSessionEntity e)
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
        MapRuntimeEntity(r.Id, r.Runtime, e);
        MapRepositoryEntity(r.Id, r.Repository, e);
        MapPullRequestEntity(r.Id, r.PullRequest, e);
    }

    private static AgentSessionRuntimeRecord? ToRuntimeRecord(AgentSessionRuntimeEntity? e)
        => e is null ? null : new AgentSessionRuntimeRecord(e.Id, e.SandboxId, e.ServiceUrl, e.CreatedAt);

    private static AgentSessionRepositoryRecord? ToRepositoryRecord(AgentSessionRepositoryEntity? e)
        => e is null ? null : new AgentSessionRepositoryRecord(e.Id, e.FullName, e.CloneUrl, e.BaseBranch, e.CredentialRef, e.Branch, e.CreatedAt);

    private static AgentSessionPullRequestRecord? ToPullRequestRecord(AgentSessionPullRequestEntity? e)
        => e is null ? null : new AgentSessionPullRequestRecord(e.Id, e.Url, e.Number, e.Branch, e.CommitSha, e.CreatedAt);

    private static AgentSessionRuntimeEntity? ToRuntimeEntity(Guid sessionId, AgentSessionRuntimeRecord? r)
        => r is null ? null : new AgentSessionRuntimeEntity
        {
            Id = r.Id,
            SessionId = sessionId,
            SandboxId = r.SandboxId,
            ServiceUrl = r.ServiceUrl,
            CreatedAt = r.CreatedAt,
        };

    private static AgentSessionRepositoryEntity? ToRepositoryEntity(Guid sessionId, AgentSessionRepositoryRecord? r)
        => r is null ? null : new AgentSessionRepositoryEntity
        {
            Id = r.Id,
            SessionId = sessionId,
            FullName = r.FullName,
            CloneUrl = r.CloneUrl,
            BaseBranch = r.BaseBranch,
            CredentialRef = r.CredentialRef,
            Branch = r.Branch,
            CreatedAt = r.CreatedAt,
        };

    private static AgentSessionPullRequestEntity? ToPullRequestEntity(Guid sessionId, AgentSessionPullRequestRecord? r)
        => r is null ? null : new AgentSessionPullRequestEntity
        {
            Id = r.Id,
            SessionId = sessionId,
            Url = r.Url,
            Number = r.Number,
            Branch = r.Branch,
            CommitSha = r.CommitSha,
            CreatedAt = r.CreatedAt,
        };

    private void MapRuntimeEntity(Guid sessionId, AgentSessionRuntimeRecord? record, AgentSessionEntity entity)
    {
        if (record is null)
        {
            if (entity.Runtime is not null)
            {
                _eaosDbContext.AgentSessionRuntimes.Remove(entity.Runtime);
                entity.Runtime = null;
            }
            return;
        }

        if (entity.Runtime is null)
        {
            entity.Runtime = new AgentSessionRuntimeEntity { Id = record.Id, SessionId = sessionId };
            _eaosDbContext.AgentSessionRuntimes.Add(entity.Runtime);
        }
        entity.Runtime.SandboxId = record.SandboxId;
        entity.Runtime.ServiceUrl = record.ServiceUrl;
        entity.Runtime.CreatedAt = record.CreatedAt;
    }

    private void MapRepositoryEntity(Guid sessionId, AgentSessionRepositoryRecord? record, AgentSessionEntity entity)
    {
        if (record is null)
        {
            if (entity.Repository is not null)
            {
                _eaosDbContext.AgentSessionRepositories.Remove(entity.Repository);
                entity.Repository = null;
            }
            return;
        }

        if (entity.Repository is null)
        {
            entity.Repository = new AgentSessionRepositoryEntity { Id = record.Id, SessionId = sessionId };
            _eaosDbContext.AgentSessionRepositories.Add(entity.Repository);
        }
        entity.Repository.FullName = record.FullName;
        entity.Repository.CloneUrl = record.CloneUrl;
        entity.Repository.BaseBranch = record.BaseBranch;
        entity.Repository.CredentialRef = record.CredentialRef;
        entity.Repository.Branch = record.Branch;
        entity.Repository.CreatedAt = record.CreatedAt;
    }

    private void MapPullRequestEntity(Guid sessionId, AgentSessionPullRequestRecord? record, AgentSessionEntity entity)
    {
        if (record is null)
        {
            if (entity.PullRequest is not null)
            {
                _eaosDbContext.AgentSessionPullRequests.Remove(entity.PullRequest);
                entity.PullRequest = null;
            }
            return;
        }

        if (entity.PullRequest is null)
        {
            entity.PullRequest = new AgentSessionPullRequestEntity { Id = record.Id, SessionId = sessionId };
            _eaosDbContext.AgentSessionPullRequests.Add(entity.PullRequest);
        }
        entity.PullRequest.Url = record.Url;
        entity.PullRequest.Number = record.Number;
        entity.PullRequest.Branch = record.Branch;
        entity.PullRequest.CommitSha = record.CommitSha;
        entity.PullRequest.CreatedAt = record.CreatedAt;
    }
}

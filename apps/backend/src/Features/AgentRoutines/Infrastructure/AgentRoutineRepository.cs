namespace OffceOs.Infrastructure.Features.AgentRoutines;

internal sealed class AgentRoutineRepository : IAgentRoutineRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentRoutineRepository(EaosDbContext eaosDbContext)
    {
        _eaosDbContext = eaosDbContext;
    }

    public async Task<IReadOnlyList<AgentRoutineRecord>> ListAsync(AgentRoutineFilter filter, CancellationToken ct = default)
    {
        var entities = await ApplyFilter(_eaosDbContext.AgentRoutines.AsNoTracking(), filter)
            .Include(routine => routine.Triggers)
            .OrderByDescending(routine => routine.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(ToAgentRoutineRecord).ToList();
    }

    public async Task<IReadOnlyList<AgentRoutineWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentRoutines
            .AsNoTracking()
            .Include(routine => routine.Triggers)
            .Join(
                _eaosDbContext.Agents.AsNoTracking(),
                routine => routine.AgentId,
                agent => agent.Id,
                (routine, agent) => new { routine, agent })
            .Where(row => !row.agent.IsDeleted);

        if (ownerId.HasValue)
            query = query.Where(row => row.agent.OwnerId == ownerId.Value);

        if (workspaceId.HasValue)
            query = query.Where(row => row.agent.WorkspaceId == workspaceId.Value);

        var rows = await query
            .OrderByDescending(row => row.routine.CreatedAt)
            .ToListAsync(ct);

        return rows
            .Select(row => new AgentRoutineWithAgentRecord(ToAgentRoutineRecord(row.routine), row.agent.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<AgentRoutineRecord>> ListAllEnabledAsync(CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentRoutines
            .AsNoTracking()
            .Include(routine => routine.Triggers)
            .Where(routine => routine.Enabled)
            .ToListAsync(ct);

        return entities.Select(ToAgentRoutineRecord).ToList();
    }

    public async Task<AgentRoutineRecord?> GetByAsync(AgentRoutineFilter filter, CancellationToken ct = default)
    {
        var entity = await ApplyFilter(_eaosDbContext.AgentRoutines.AsNoTracking(), filter)
            .Include(routine => routine.Triggers)
            .FirstOrDefaultAsync(ct);

        return entity is null ? null : ToAgentRoutineRecord(entity);
    }

    public async Task<AgentRoutineWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.AgentRoutines
            .AsNoTracking()
            .Include(routine => routine.Triggers)
            .Join(
                _eaosDbContext.Agents.AsNoTracking(),
                routine => routine.AgentId,
                agent => agent.Id,
                (routine, agent) => new { routine, agent })
            .Where(row => row.routine.Id == id && !row.agent.IsDeleted);

        if (ownerId.HasValue)
            query = query.Where(row => row.agent.OwnerId == ownerId.Value);

        if (workspaceId.HasValue)
            query = query.Where(row => row.agent.WorkspaceId == workspaceId.Value);

        var row = await query.FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new AgentRoutineWithAgentRecord(ToAgentRoutineRecord(row.routine), row.agent.Name);
    }

    public async Task<AgentRoutineTriggerRecord?> GetTriggerByAsync(Guid triggerId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentRoutineTriggers
            .AsNoTracking()
            .FirstOrDefaultAsync(trigger => trigger.Id == triggerId, ct);

        return entity is null ? null : ToAgentRoutineTriggerRecord(entity);
    }

    public async Task<AgentRoutineRecord> UpsertAsync(AgentRoutineRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentRoutines
            .Include(routine => routine.Triggers)
            .FirstOrDefaultAsync(routine => routine.Id == record.Id, ct);

        if (entity is null)
        {
            entity = ToAgentRoutineEntity(record);
            _eaosDbContext.AgentRoutines.Add(entity);
        }
        else
        {
            MapToAgentRoutineEntity(record, entity);
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToAgentRoutineRecord(entity);
    }

    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentRoutines.FirstOrDefaultAsync(routine => routine.Id == id, ct)
            ?? throw new InvalidOperationException("Routine not found.");

        entity.Enabled = enabled;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentRoutines.FirstOrDefaultAsync(routine => routine.Id == id, ct);
        if (entity is null) return false;

        _eaosDbContext.AgentRoutines.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<AgentRoutineEntity> ApplyFilter(IQueryable<AgentRoutineEntity> query, AgentRoutineFilter filter)
    {
        if (filter.Id.HasValue)
            query = query.Where(routine => routine.Id == filter.Id.Value);

        if (filter.AgentId.HasValue)
            query = query.Where(routine => routine.AgentId == filter.AgentId.Value);

        if (filter.Enabled.HasValue)
            query = query.Where(routine => routine.Enabled == filter.Enabled.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(routine => routine.Agent != null && routine.Agent.WorkspaceId == filter.WorkspaceId.Value);

        return query;
    }

    private static AgentRoutineRecord ToAgentRoutineRecord(AgentRoutineEntity entity) => new()
    {
        Id = entity.Id,
        AgentId = entity.AgentId,
        Name = entity.Name,
        Prompt = entity.Prompt,
        Enabled = entity.Enabled,
        LastTriggeredAt = entity.LastTriggeredAt,
        CreatedAt = entity.CreatedAt,
        Triggers = entity.Triggers.OrderBy(trigger => trigger.CreatedAt).Select(ToAgentRoutineTriggerRecord).ToList(),
    };

    private static AgentRoutineTriggerRecord ToAgentRoutineTriggerRecord(AgentRoutineTriggerEntity entity) => new()
    {
        Id = entity.Id,
        RoutineId = entity.RoutineId,
        Kind = entity.Kind,
        Name = entity.Name,
        Enabled = entity.Enabled,
        ConfigJson = entity.ConfigJson,
        SecretHash = entity.SecretHash,
        EncryptedSecret = entity.EncryptedSecret,
        LastTriggeredAt = entity.LastTriggeredAt,
        NextRunAt = entity.NextRunAt,
        CreatedAt = entity.CreatedAt,
    };

    private static AgentRoutineEntity ToAgentRoutineEntity(AgentRoutineRecord record) => new()
    {
        Id = record.Id,
        AgentId = record.AgentId,
        Name = record.Name,
        Prompt = record.Prompt,
        Enabled = record.Enabled,
        LastTriggeredAt = record.LastTriggeredAt,
        CreatedAt = record.CreatedAt,
        Triggers = record.Triggers.Select(ToAgentRoutineTriggerEntity).ToList(),
    };

    private static AgentRoutineTriggerEntity ToAgentRoutineTriggerEntity(AgentRoutineTriggerRecord record) => new()
    {
        Id = record.Id,
        RoutineId = record.RoutineId,
        Kind = record.Kind,
        Name = record.Name,
        Enabled = record.Enabled,
        ConfigJson = record.ConfigJson,
        SecretHash = record.SecretHash,
        EncryptedSecret = record.EncryptedSecret,
        LastTriggeredAt = record.LastTriggeredAt,
        NextRunAt = record.NextRunAt,
        CreatedAt = record.CreatedAt,
    };

    private static void MapToAgentRoutineEntity(AgentRoutineRecord record, AgentRoutineEntity entity)
    {
        entity.Name = record.Name;
        entity.Prompt = record.Prompt;
        entity.Enabled = record.Enabled;
        entity.LastTriggeredAt = record.LastTriggeredAt;

        var existingById = entity.Triggers.ToDictionary(trigger => trigger.Id);
        foreach (var triggerRecord in record.Triggers)
        {
            if (existingById.TryGetValue(triggerRecord.Id, out var triggerEntity))
                MapToAgentRoutineTriggerEntity(triggerRecord, triggerEntity);
            else
                entity.Triggers.Add(ToAgentRoutineTriggerEntity(triggerRecord));
        }

        var recordTriggerIds = record.Triggers.Select(trigger => trigger.Id).ToHashSet();
        var removed = entity.Triggers.Where(trigger => !recordTriggerIds.Contains(trigger.Id)).ToList();
        foreach (var trigger in removed)
            entity.Triggers.Remove(trigger);
    }

    private static void MapToAgentRoutineTriggerEntity(AgentRoutineTriggerRecord record, AgentRoutineTriggerEntity entity)
    {
        entity.Name = record.Name;
        entity.Enabled = record.Enabled;
        entity.ConfigJson = record.ConfigJson;
        entity.SecretHash = record.SecretHash;
        entity.EncryptedSecret = record.EncryptedSecret;
        entity.LastTriggeredAt = record.LastTriggeredAt;
        entity.NextRunAt = record.NextRunAt;
    }
}

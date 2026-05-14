namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class AgentResourceRepository : IAgentResourceRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentResourceRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task AttachToSessionAsync(AgentSessionResourceAttachmentRecord attachment, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.AgentSessionResourceAttachments.FirstOrDefaultAsync(
            a => a.SessionId == attachment.SessionId
                && a.ResourceType == attachment.ResourceType
                && a.ResourceId == attachment.ResourceId,
            ct);

        if (existing is null)
        {
            _eaosDbContext.AgentSessionResourceAttachments.Add(ToAttachmentEntity(attachment));
        }
        else
        {
            existing.AccessMode = attachment.AccessMode;
            existing.Instructions = attachment.Instructions;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AgentSessionResourceAttachmentRecord>> ListSessionAttachmentsAsync(Guid sessionId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentSessionResourceAttachments
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToAttachmentRecord).ToList();
    }

    public async Task<AgentSessionResourceAttachmentRecord?> GetActiveMemoryStoreAttachmentAsync(Guid agentId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.AgentSessionResourceAttachments
            .AsNoTracking()
            .Join(
                _eaosDbContext.AgentSessions.AsNoTracking(),
                attachment => attachment.SessionId,
                session => session.Id,
                (attachment, session) => new { attachment, session })
            .Where(x => x.attachment.AgentId == agentId
                && x.attachment.ResourceType == AgentResourceKinds.MemoryStore
                && x.session.Status == SessionStatus.Active.ToStorageString())
            .OrderByDescending(x => x.attachment.CreatedAt)
            .Select(x => x.attachment)
            .FirstOrDefaultAsync(ct);
        return entity is null ? null : ToAttachmentRecord(entity);
    }

    private static AgentSessionResourceAttachmentRecord ToAttachmentRecord(AgentSessionResourceAttachmentEntity e) => new()
    {
        Id = e.Id,
        AgentId = e.AgentId,
        SessionId = e.SessionId,
        ResourceType = e.ResourceType,
        ResourceId = e.ResourceId,
        AccessMode = e.AccessMode,
        Instructions = e.Instructions,
        CreatedAt = e.CreatedAt,
    };

    private static AgentSessionResourceAttachmentEntity ToAttachmentEntity(AgentSessionResourceAttachmentRecord r) => new()
    {
        Id = r.Id,
        AgentId = r.AgentId,
        SessionId = r.SessionId,
        ResourceType = r.ResourceType,
        ResourceId = r.ResourceId,
        AccessMode = r.AccessMode,
        Instructions = r.Instructions,
        CreatedAt = r.CreatedAt,
    };
}

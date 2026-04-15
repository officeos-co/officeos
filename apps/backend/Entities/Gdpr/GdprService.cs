using EnterpriseAgentOs.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Entities.Gdpr;

public sealed class GdprService : IGdprService
{
    private readonly EaosDbContext _db;

    public GdprService(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<GdprExportDto> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var userDto = new GdprUserDto(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt,
            user.LastLoginAt);

        // All agents owned by the user (not soft-deleted)
        var agentIds = await _db.Agents
            .AsNoTracking()
            .Where(a => a.OwnerId == userId && !a.IsDeleted)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var agents = await _db.Agents
            .AsNoTracking()
            .Where(a => a.OwnerId == userId && !a.IsDeleted)
            .Select(a => new GdprAgentDto(a.Id, a.Name, a.Provider, a.Model, a.Status, a.CreatedAt))
            .ToListAsync(ct);

        // Conversations for all user's agents
        var conversations = agentIds.Count == 0
            ? new List<GdprConversationDto>()
            : await _db.AgentConversations
                .AsNoTracking()
                .Where(c => agentIds.Contains(c.AgentId))
                .Select(c => new GdprConversationDto(c.Id, c.AgentId, c.Role, c.Content, c.SessionId, c.CreatedAt))
                .ToListAsync(ct);

        // Memories for all user's agents
        var memories = agentIds.Count == 0
            ? new List<GdprMemoryDto>()
            : await _db.AgentMemories
                .AsNoTracking()
                .Where(m => agentIds.Contains(m.AgentId))
                .Select(m => new GdprMemoryDto(m.Id, m.AgentId, m.Key, m.Content, m.Category, m.Namespace, m.CreatedAt))
                .ToListAsync(ct);

        // Audit log entries for all user's agents
        var auditEntries = agentIds.Count == 0
            ? new List<GdprAuditEntryDto>()
            : await _db.AgentToolCalls
                .AsNoTracking()
                .Where(t => agentIds.Contains(t.AgentId))
                .Select(t => new GdprAuditEntryDto(
                    t.Id, t.AgentId, t.SkillName, t.Action, t.ParamsJson, t.ResultSummary, t.DurationMs, t.Timestamp))
                .ToListAsync(ct);

        // Skill credentials — export metadata only, never plaintext values
        var skillCredentials = await _db.SkillCredentials
            .AsNoTracking()
            .Select(s => new GdprSkillCredentialDto(s.Id, s.SkillName, s.Enabled, s.ConfiguredAt))
            .ToListAsync(ct);

        return new GdprExportDto(userDto, agents, conversations, memories, auditEntries, skillCredentials);
    }

    public async Task PurgeAsync(Guid userId, CancellationToken ct = default)
    {
        // Collect all agent IDs owned by this user
        var agentIds = await _db.Agents
            .Where(a => a.OwnerId == userId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (agentIds.Count > 0)
        {
            // 1. AgentToolCallRecord entries for all user's agents
            await _db.AgentToolCalls
                .Where(t => agentIds.Contains(t.AgentId))
                .ExecuteDeleteAsync(ct);

            // 2. AgentConversationRecord entries for all user's agents
            await _db.AgentConversations
                .Where(c => agentIds.Contains(c.AgentId))
                .ExecuteDeleteAsync(ct);

            // 3. AgentMemoryRecord entries for all user's agents
            await _db.AgentMemories
                .Where(m => agentIds.Contains(m.AgentId))
                .ExecuteDeleteAsync(ct);

            // 4. AgentRecord rows owned by user
            await _db.Agents
                .Where(a => a.OwnerId == userId)
                .ExecuteDeleteAsync(ct);
        }

        // 5. SkillCredentials — single-tenant install, they are shared across all users.
        //    Per-spec: skip if shared. In this single-tenant installation skill credentials
        //    are global (no OwnerId), so we leave them intact.

        // 6. SessionRecord for the user
        await _db.Sessions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // 7. UserRecord
        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(ct);
    }
}

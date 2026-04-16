using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Database.Models;
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

        // Conversations for all user's agents — sourced from AgentLogs (message entries).
        var conversations = agentIds.Count == 0
            ? new List<GdprConversationDto>()
            : await _db.AgentLogs
                .AsNoTracking()
                .Where(l => agentIds.Contains(l.AgentId) &&
                    (l.Type == AgentLogType.MessageIn ||
                     l.Type == AgentLogType.MessageOut ||
                     l.Type == AgentLogType.System))
                .Select(l => new GdprConversationDto(
                    l.Id,
                    l.AgentId,
                    l.Type == AgentLogType.MessageIn ? "user"
                        : l.Type == AgentLogType.MessageOut ? "assistant"
                        : "system",
                    l.Content,
                    l.CorrelationId,
                    l.Time))
                .ToListAsync(ct);

        // Memories for all user's agents
        var memories = agentIds.Count == 0
            ? new List<GdprMemoryDto>()
            : await _db.AgentMemories
                .AsNoTracking()
                .Where(m => agentIds.Contains(m.AgentId))
                .Select(m => new GdprMemoryDto(m.Id, m.AgentId, m.Key, m.Content, m.Category, m.Namespace, m.CreatedAt))
                .ToListAsync(ct);

        // Audit log entries for all user's agents — sourced from AgentLogs (ToolCall entries).
        var auditEntries = agentIds.Count == 0
            ? new List<GdprAuditEntryDto>()
            : await _db.AgentLogs
                .AsNoTracking()
                .Where(l => agentIds.Contains(l.AgentId) && l.Type == AgentLogType.ToolCall)
                .Select(l => new GdprAuditEntryDto(
                    l.Id,
                    l.AgentId,
                    l.Integration ?? string.Empty,
                    l.Tool ?? string.Empty,
                    l.Content,
                    null,
                    (long)(l.DurationMs ?? 0),
                    l.Time))
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
            // 1+2. AgentLogRecord entries for all user's agents (tool calls + messages)
            await _db.AgentLogs
                .Where(l => agentIds.Contains(l.AgentId))
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

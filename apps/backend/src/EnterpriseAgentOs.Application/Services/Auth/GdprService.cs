namespace EnterpriseAgentOs.Application.Services.Auth;

public sealed class GdprService : IGdprService
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public GdprService(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
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

        var conversations = agentIds.Count == 0
            ? new List<GdprConversationDto>()
            : await _db.AgentLogs
                .AsNoTracking()
                .Where(l => agentIds.Contains(l.AgentId) &&
                    (l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.MessageIn ||
                     l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.MessageOut ||
                     l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.System))
                .Select(l => new GdprConversationDto(
                    l.Id,
                    l.AgentId,
                    l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.MessageIn ? "user"
                        : l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.MessageOut ? "assistant"
                        : "system",
                    l.Content,
                    l.CorrelationId,
                    l.Time))
                .ToListAsync(ct);

        var auditEntries = agentIds.Count == 0
            ? new List<GdprAuditEntryDto>()
            : await _db.AgentLogs
                .AsNoTracking()
                .Where(l => agentIds.Contains(l.AgentId) && l.Type == EnterpriseAgentOs.Domain.Models.AgentLogType.ToolCall)
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

        var skillCredentials = await _db.SkillCredentials
            .AsNoTracking()
            .Select(s => new GdprSkillCredentialDto(s.Id, s.SkillName, s.Enabled, s.ConfiguredAt))
            .ToListAsync(ct);

        return new GdprExportDto(userDto, agents, conversations, auditEntries, skillCredentials);
    }

    public async Task PurgeAsync(Guid userId, CancellationToken ct = default)
    {
        var agentIds = await _db.Agents
            .Where(a => a.OwnerId == userId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (agentIds.Count > 0)
        {
            await _db.AgentLogs
                .Where(l => agentIds.Contains(l.AgentId))
                .ExecuteDeleteAsync(ct);

            await _db.Agents
                .Where(a => a.OwnerId == userId)
                .ExecuteDeleteAsync(ct);
        }

        await _db.Sessions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);

        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(ct);
    }
}

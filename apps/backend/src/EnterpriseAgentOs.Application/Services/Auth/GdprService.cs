namespace EnterpriseAgentOs.Application.Services.Auth;

public sealed class GdprService : IGdprService
{
    private readonly IUserRepository _users;
    private readonly IAgentRepository _agents;
    private readonly IAgentLogRepository _agentLogs;
    private readonly ISessionRepository _sessions;
    private readonly ISkillRepository _skills;

    public GdprService(
        IUserRepository users,
        IAgentRepository agents,
        IAgentLogRepository agentLogs,
        ISessionRepository sessions,
        ISkillRepository skills)
    {
        _users = users;
        _agents = agents;
        _agentLogs = agentLogs;
        _sessions = sessions;
        _skills = skills;
    }

    public async Task<GdprExportDto> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var userDto = new GdprUserDto(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt,
            user.LastLoginAt);

        var agentRecords = await _agents.ListByOwnerAsync(userId, includeDeleted: false, ct);
        var agentIds = agentRecords.Select(a => a.Id).ToList();

        var agents = agentRecords
            .Select(a => new GdprAgentDto(a.Id, a.Name, a.Provider, a.Model, a.Status, a.CreatedAt))
            .ToList();

        var conversationTypes = new List<AgentLogType>
        {
            AgentLogType.MessageIn,
            AgentLogType.MessageOut,
            AgentLogType.System
        };
        var conversationLogs = await _agentLogs.ListByAgentIdsAsync(agentIds, conversationTypes, ct);
        var conversations = conversationLogs
            .Select(l => new GdprConversationDto(
                l.Id,
                l.AgentId,
                l.Type == AgentLogType.MessageIn ? "user"
                    : l.Type == AgentLogType.MessageOut ? "assistant"
                    : "system",
                l.Content,
                l.CorrelationId,
                l.Time))
            .ToList();

        var toolCallTypes = new List<AgentLogType> { AgentLogType.ToolCall };
        var toolCallLogs = await _agentLogs.ListByAgentIdsAsync(agentIds, toolCallTypes, ct);
        var auditEntries = toolCallLogs
            .Select(l => new GdprAuditEntryDto(
                l.Id,
                l.AgentId,
                l.Integration ?? string.Empty,
                l.Tool ?? string.Empty,
                l.Content,
                null,
                (long)(l.DurationMs ?? 0),
                l.Time))
            .ToList();

        var skillCredentials = (await _skills.ListAsync(ct))
            .Select(s => new GdprSkillCredentialDto(s.Id, s.SkillName, s.Enabled, s.ConfiguredAt))
            .ToList();

        return new GdprExportDto(userDto, agents, conversations, auditEntries, skillCredentials);
    }

    public async Task PurgeAsync(Guid userId, CancellationToken ct = default)
    {
        var agentRecords = await _agents.ListByOwnerAsync(userId, includeDeleted: true, ct);
        var agentIds = agentRecords.Select(a => a.Id).ToList();

        if (agentIds.Count > 0)
        {
            await _agentLogs.DeleteByAgentIdsAsync(agentIds, ct);
            await _agents.HardDeleteByOwnerAsync(userId, ct);
        }

        await _sessions.DeleteByUserIdAsync(userId, ct);
        await _users.DeleteAsync(userId, ct);
    }
}

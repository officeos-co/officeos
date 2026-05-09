namespace OffceOs.Application.Features.Management;

internal sealed class GdprService : IGdprService
{
    private readonly IUserRepository _userRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentLogRepository _agentLogRepository;
    private readonly ISessionRepository _sessionRepository;

    public GdprService(
        IUserRepository users,
        IAgentRepository agents,
        IAgentLogRepository agentLogs,
        ISessionRepository sessions)
    {
        _userRepository = users;
        _agentRepository = agents;
        _agentLogRepository = agentLogs;
        _sessionRepository = sessions;
    }

    public async Task<GdprExport> ExportAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByAsync(new UserFilter { Id = userId }, ct)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var userExport = new GdprUserExport(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt,
            user.LastLoginAt);

        var agentRecords = await _agentRepository.ListAsync(new AgentFilter { OwnerId = userId }, ct);
        var agentIds = agentRecords.Select(a => a.Id).ToList();

        var agents = agentRecords
            .Select(a => new GdprAgentExport(a.Id, a.Name, a.Provider, a.Model, a.Status.ToStorageString(), a.CreatedAt))
            .ToList();

        var conversationTypes = new List<AgentLogType>
        {
            AgentLogType.MessageIn,
            AgentLogType.MessageOut,
            AgentLogType.System
        };
        var conversationLogs = await _agentLogRepository.ListAsync(
            new AgentLogFilter { AgentIds = agentIds, Types = conversationTypes },
            new AgentLogListOptions { Sort = AgentLogSort.TimeAscending },
            ct);
        var conversations = conversationLogs
            .Where(l => l.AgentId.HasValue)
            .Select(l => new GdprConversationExport(
                l.Id,
                l.AgentId!.Value,
                l.Type == AgentLogType.MessageIn ? "user"
                    : l.Type == AgentLogType.MessageOut ? "assistant"
                    : "system",
                l.Content,
                l.CorrelationId,
                l.Time))
            .ToList();

        var toolCallTypes = new List<AgentLogType> { AgentLogType.ToolCall };
        var toolCallLogs = await _agentLogRepository.ListAsync(
            new AgentLogFilter { AgentIds = agentIds, Types = toolCallTypes },
            new AgentLogListOptions { Sort = AgentLogSort.TimeAscending },
            ct);
        var auditEntries = toolCallLogs
            .Where(l => l.AgentId.HasValue)
            .Select(l => new GdprAuditEntryExport(
                l.Id,
                l.AgentId!.Value,
                l.Integration ?? string.Empty,
                l.Tool ?? string.Empty,
                l.Content,
                null,
                (long)(l.Usage.DurationMs ?? 0),
                l.Time))
            .ToList();

        return new GdprExport(userExport, agents, conversations, auditEntries);
    }

    public async Task PurgeAsync(Guid userId, CancellationToken ct = default)
    {
        var agentRecords = await _agentRepository.ListAsync(new AgentFilter { OwnerId = userId, IncludeDeleted = true }, ct);
        var agentIds = agentRecords.Select(a => a.Id).ToList();

        if (agentIds.Count > 0)
        {
            await _agentLogRepository.DeleteByAgentIdsAsync(agentIds, ct);
            await _agentRepository.HardDeleteAsync(new AgentFilter { OwnerId = userId, IncludeDeleted = true }, ct);
        }

        await _sessionRepository.DeleteByUserIdAsync(userId, ct);
        await _userRepository.DeleteAsync(userId, ct);
    }
}

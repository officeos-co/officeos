namespace EnterpriseAgentOs.Api.Entities.AgentLogs;

public sealed record GlobalLogRow(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord Log, string AgentName);

public interface IAgentLogRepository
{
    Task<List<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, EnterpriseAgentOs.Api.Database.Models.AgentLogType? type, int skip, int limit, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Api.Database.Models.AgentLogRecord record, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

namespace EnterpriseAgentOs.Domain.Interfaces.AgentLogs;

public sealed record GlobalLogRow(EnterpriseAgentOs.Domain.Models.AgentLogRecord Log, string AgentName);

public interface IAgentLogRepository
{
    Task<List<EnterpriseAgentOs.Domain.Models.AgentLogRecord>> ListAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default);
    Task<(List<GlobalLogRow> Items, int Total)> ListGlobalAsync(
        string? search, string? agentName, EnterpriseAgentOs.Domain.Models.AgentLogType? type, int skip, int limit, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Domain.Models.AgentLogRecord record, CancellationToken ct = default);
    Task AppendPairAsync(EnterpriseAgentOs.Domain.Models.AgentLogRecord toolCall, EnterpriseAgentOs.Domain.Models.AgentLogRecord toolResult, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(List<EnterpriseAgentOs.Domain.Models.AgentLogRecord> Items, int Total)> GetToolCallsAsync(Guid agentId, int limit, int offset, CancellationToken ct = default);
    Task<Dictionary<string, EnterpriseAgentOs.Domain.Models.AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default);
}

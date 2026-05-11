namespace OffceOs.Domain.Features.AgentUsage;

public interface IAgentUsageRepository
{
    IQueryable<AgentUsageCallRecord> Query(AgentUsageFilter filter);
    Task<List<AgentUsageCallRecord>> ListAsync(AgentUsageFilter filter, CancellationToken ct = default);
    Task<AgentUsageCallRecord> SaveAsync(AgentUsageCallRecord record, CancellationToken ct = default);
}

namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record AgentRunFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public Guid? ParentRunId { get; init; }
    public string? Status { get; init; }
}

public interface IAgentRunRepository
{
    Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default);
    Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default);
    Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default);
}

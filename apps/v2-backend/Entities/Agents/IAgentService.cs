using EnterpriseAgentOs.Api.Entities.Agents;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public interface IAgentService
{
    Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default);
    Task<AgentDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AgentDto> CreateAsync(CreateAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

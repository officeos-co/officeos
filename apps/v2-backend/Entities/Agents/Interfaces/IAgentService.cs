using EnterpriseAgentOs.Api.Entities.Agents.Models;

namespace EnterpriseAgentOs.Api.Entities.Agents.Interfaces;

public interface IAgentService
{
    Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default);
    Task<AgentDto> CreateAsync(CreateAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

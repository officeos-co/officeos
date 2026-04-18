namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface IAgentSkillRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListSkillNamesByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, IEnumerable<string> skillNames, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default);
}

namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface IAgentSkillRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListSkillNamesByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, IEnumerable<string> skillNames, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid agentId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord> UpsertToolPermissionAsync(Guid agentId, string skillName, string toolName, EnterpriseAgentOs.Domain.Models.ToolPermission permission, CancellationToken ct = default);
}

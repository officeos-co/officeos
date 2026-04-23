namespace EnterpriseAgentOs.Domain.Features.AgentSkills;

public interface IAgentSkillRepository
{
    Task<IReadOnlyList<AgentSkillRecord>> ListByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListSkillNamesByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<SkillRecord>> ListSkillDetailsForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task AssignAsync(Guid agentId, IEnumerable<string> skillNames, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid agentId, string skillName, CancellationToken ct = default);
    Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid agentId, CancellationToken ct = default);
    Task<AgentToolPermissionRecord> UpsertToolPermissionAsync(Guid agentId, string skillName, string toolName, ToolPermission permission, CancellationToken ct = default);
}

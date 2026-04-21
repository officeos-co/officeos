namespace EnterpriseAgentOs.Api.Agents;

public sealed record AgentSkillDto(
    string SkillName,
    IReadOnlyList<AgentToolPermissionRecord> Permissions);

namespace EnterpriseAgentOs.Api.Features.Agents;

public sealed record AgentSkillGqlDto(
    string SkillName,
    IReadOnlyList<AgentToolPermissionRecord> Permissions);

namespace EnterpriseAgentOs.Api.Entities.AgentSkills.Types;

public sealed record AgentToolPermissionDto(
    string SkillName,
    string ToolName,
    EnterpriseAgentOs.Api.Database.Models.ToolPermission Permission);

public sealed record AgentSkillDto(
    string SkillName,
    IReadOnlyList<AgentToolPermissionDto> Permissions);

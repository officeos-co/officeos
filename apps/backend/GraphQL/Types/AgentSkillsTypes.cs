namespace EnterpriseAgentOs.Api.GraphQL.Types;

public sealed record AgentToolPermissionDto(
    string SkillName,
    string ToolName,
    EnterpriseAgentOs.Domain.Models.ToolPermission Permission);

public sealed record AgentSkillDto(
    string SkillName,
    IReadOnlyList<AgentToolPermissionDto> Permissions);

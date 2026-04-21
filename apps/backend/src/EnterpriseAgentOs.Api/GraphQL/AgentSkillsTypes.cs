namespace EnterpriseAgentOs.Api.GraphQL;

public sealed record AgentToolPermissionDto(
    string SkillName,
    string ToolName,
    ToolPermission Permission);

public sealed record AgentSkillDto(
    string SkillName,
    IReadOnlyList<AgentToolPermissionDto> Permissions);

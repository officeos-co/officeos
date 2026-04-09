namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed record SkillDto(Guid Id, string Name, string DisplayName, bool Installed);

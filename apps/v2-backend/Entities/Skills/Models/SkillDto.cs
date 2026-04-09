namespace EnterpriseAgentOs.Api.Entities.Skills.Models;

public sealed record SkillDto(Guid Id, string Name, string DisplayName, bool Installed);

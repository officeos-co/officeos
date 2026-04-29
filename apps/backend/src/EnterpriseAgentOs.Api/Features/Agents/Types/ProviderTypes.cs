namespace EnterpriseAgentOs.Api.Features.Agents;

public sealed record ModelInfoDto(
    string Id,
    string DisplayName,
    bool IsDefault);

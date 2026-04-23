namespace EnterpriseAgentOs.Api.Features.Providers;

public sealed record ModelInfoDto(
    string Id,
    string DisplayName,
    bool IsDefault);

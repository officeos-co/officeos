namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed record ProviderDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt);

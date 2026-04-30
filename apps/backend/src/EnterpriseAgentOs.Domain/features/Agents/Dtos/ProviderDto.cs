namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record ProviderDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt);

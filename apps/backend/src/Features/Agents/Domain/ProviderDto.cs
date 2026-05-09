namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record ProviderDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt,
    IReadOnlyList<ProviderModelDto> Models);

public sealed record ProviderModelDto(
    string Id,
    string DisplayName,
    int CostWeight);

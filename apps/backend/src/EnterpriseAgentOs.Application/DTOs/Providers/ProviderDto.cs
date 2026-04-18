namespace EnterpriseAgentOs.Application.DTOs.Providers;

public sealed record ProviderDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt);

public sealed record ConfigureProviderRequest(
    [Required, MinLength(1)] string ApiKey);

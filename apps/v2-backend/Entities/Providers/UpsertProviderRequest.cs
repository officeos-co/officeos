
namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed record ConfigureProviderRequest(
    [Required, MinLength(1)] string ApiKey);

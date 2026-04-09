namespace EnterpriseAgentOs.Api.Entities.Providers.Models;

public sealed record ProviderDto(Guid Id, string Name, string DisplayName, bool Configured);

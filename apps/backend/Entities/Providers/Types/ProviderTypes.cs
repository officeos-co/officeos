namespace EnterpriseAgentOs.Api.Entities.Providers.Types;

public sealed record ProviderGqlDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt);

internal static class ProviderGraphQLMapper
{
    public static ProviderGqlDto ToDto(EnterpriseAgentOs.Api.Entities.Providers.ProviderDto p) =>
        new(p.Id, p.Name, p.DisplayName, p.Configured, p.ConfiguredAt);
}

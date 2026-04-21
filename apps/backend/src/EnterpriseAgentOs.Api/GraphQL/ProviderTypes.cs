namespace EnterpriseAgentOs.Api.GraphQL;

public sealed record ProviderGqlDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt);

public sealed record ModelInfoDto(
    string Id,
    string DisplayName,
    bool IsDefault);

internal static class ProviderGraphQLMapper
{
    public static ProviderGqlDto ToDto(ProviderDto p) =>
        new(p.Id, p.Name, p.DisplayName, p.Configured, p.ConfiguredAt);
}

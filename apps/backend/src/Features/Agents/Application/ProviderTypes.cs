namespace EnterpriseAgentOs.Application.Features.Agents;

public sealed record ProviderGqlDto(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt,
    IReadOnlyList<string> Models);

internal static class ProviderGraphQLMapper
{
    public static ProviderGqlDto ToDto(ProviderResult p) =>
        new(p.Id, p.Name, p.DisplayName, p.Configured, p.ConfiguredAt,
            p.Models.Select(m => m.Id).ToList());
}

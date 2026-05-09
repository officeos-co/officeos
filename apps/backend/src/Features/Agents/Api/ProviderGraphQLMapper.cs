namespace OffceOs.Api.Features.Agents;

public sealed record ProviderPayload(
    Guid Id,
    string Name,
    string DisplayName,
    bool Configured,
    DateTime? ConfiguredAt,
    IReadOnlyList<string> Models);

internal static class ProviderGraphQLMapper
{
    public static ProviderPayload ToPayload(ProviderResult p) =>
        new(p.Id, p.Name, p.DisplayName, p.Configured, p.ConfiguredAt,
            p.Models.Select(m => m.Id).ToList());
}

namespace OffceOs.Api.Features.Providers;

public sealed record OrganizationProviderProfilePayload(
    Guid Id,
    Guid OrganizationId,
    string Provider,
    string DisplayName,
    IReadOnlyList<string> AllowedModels,
    bool Enabled,
    DateTime ConfiguredAt);

public sealed record SaveOrganizationProviderProfileInput(
    Guid OrganizationId,
    string Provider,
    string DisplayName,
    IReadOnlyList<string> AllowedModels,
    string ApiKey,
    bool Enabled);

internal static class OrganizationProviderProfileGraphQLMapper
{
    public static OrganizationProviderProfilePayload ToPayload(OrganizationProviderProfileRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.Provider,
        record.DisplayName,
        ParseList(record.AllowedModelsJson),
        record.Enabled,
        record.ConfiguredAt);

    private static IReadOnlyList<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }
}

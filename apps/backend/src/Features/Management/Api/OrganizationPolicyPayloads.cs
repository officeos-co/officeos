namespace OffceOs.Api.Features.Management;

public sealed record OrganizationPolicyProfilePayload(
    Guid Id,
    Guid OrganizationId,
    bool BrowserToolsEnabled,
    bool NetworkToolsEnabled,
    bool ShellToolsEnabled,
    bool FileWriteToolsEnabled,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> DeniedTools,
    IReadOnlyList<string> AllowedIntegrations,
    IReadOnlyList<string> DeniedIntegrations,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record UpdateOrganizationPolicyProfileInput(
    Guid OrganizationId,
    bool BrowserToolsEnabled,
    bool NetworkToolsEnabled,
    bool ShellToolsEnabled,
    bool FileWriteToolsEnabled,
    IReadOnlyList<string>? AllowedTools,
    IReadOnlyList<string>? DeniedTools,
    IReadOnlyList<string>? AllowedIntegrations,
    IReadOnlyList<string>? DeniedIntegrations);

internal static class OrganizationPolicyGraphQLMapper
{
    public static OrganizationPolicyProfilePayload ToPayload(OrganizationPolicyProfileRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.BrowserToolsEnabled,
        record.NetworkToolsEnabled,
        record.ShellToolsEnabled,
        record.FileWriteToolsEnabled,
        ParseList(record.AllowedToolsJson),
        ParseList(record.DeniedToolsJson),
        ParseList(record.AllowedIntegrationsJson),
        ParseList(record.DeniedIntegrationsJson),
        record.CreatedAt,
        record.UpdatedAt);

    public static OrganizationPolicyProfileRecord ToRecord(UpdateOrganizationPolicyProfileInput input) => new()
    {
        OrganizationId = input.OrganizationId,
        BrowserToolsEnabled = input.BrowserToolsEnabled,
        NetworkToolsEnabled = input.NetworkToolsEnabled,
        ShellToolsEnabled = input.ShellToolsEnabled,
        FileWriteToolsEnabled = input.FileWriteToolsEnabled,
        AllowedToolsJson = JsonSerializer.Serialize(input.AllowedTools ?? []),
        DeniedToolsJson = JsonSerializer.Serialize(input.DeniedTools ?? []),
        AllowedIntegrationsJson = JsonSerializer.Serialize(input.AllowedIntegrations ?? []),
        DeniedIntegrationsJson = JsonSerializer.Serialize(input.DeniedIntegrations ?? []),
    };

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

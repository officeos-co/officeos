namespace OffceOs.Domain.Features.Management;

public sealed class OrganizationPolicyProfileRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public bool BrowserToolsEnabled { get; set; } = true;
    public bool NetworkToolsEnabled { get; set; } = true;
    public bool ShellToolsEnabled { get; set; } = true;
    public bool FileWriteToolsEnabled { get; set; } = true;
    public string AllowedToolsJson { get; set; } = "[]";
    public string DeniedToolsJson { get; set; } = "[]";
    public string AllowedIntegrationsJson { get; set; } = "[]";
    public string DeniedIntegrationsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public IReadOnlySet<string> AllowedTools => ParseStringSet(AllowedToolsJson);
    public IReadOnlySet<string> DeniedTools => ParseStringSet(DeniedToolsJson);
    public IReadOnlySet<string> AllowedIntegrations => ParseStringSet(AllowedIntegrationsJson);
    public IReadOnlySet<string> DeniedIntegrations => ParseStringSet(DeniedIntegrationsJson);

    public static OrganizationPolicyProfileRecord Default(Guid organizationId) => new()
    {
        OrganizationId = organizationId,
    };

    private static IReadOnlySet<string> ParseStringSet(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray()
                    .Select(value => value.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

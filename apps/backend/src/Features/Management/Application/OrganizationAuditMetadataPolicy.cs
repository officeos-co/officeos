namespace OffceOs.Application.Features.Management;

public static class OrganizationAuditMetadataPolicy
{
    private static readonly string[] SecretKeyFragments =
    [
        "apikey",
        "api_key",
        "authorization",
        "bearer",
        "clientsecret",
        "client_secret",
        "credential",
        "password",
        "privatekey",
        "private_key",
        "secret",
        "sessiontoken",
        "session_token",
        "token"
    ];

    public static string RedactJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return "{}";

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var redacted = RedactElement(document.RootElement, null);
            return JsonSerializer.Serialize(redacted);
        }
        catch
        {
            return "{}";
        }
    }

    public static string FromMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        var sorted = metadata
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return RedactJson(JsonSerializer.Serialize(sorted));
    }

    private static object? RedactElement(JsonElement element, string? key)
    {
        if (key is not null && IsSecretLike(key))
            return "[redacted]";

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToDictionary(property => property.Name, property => RedactElement(property.Value, property.Name)),
            JsonValueKind.Array => element.EnumerateArray().Select(item => RedactElement(item, key)).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static bool IsSecretLike(string key)
    {
        var normalized = key.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return SecretKeyFragments.Any(fragment =>
            normalized.Contains(fragment.Replace("_", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal));
    }
}

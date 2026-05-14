namespace OffceOs.Domain.Features.Providers;

public sealed record ProviderAuthResult(
    ProviderAuthKind Kind,
    IReadOnlyDictionary<string, string> Credentials)
{
    public string? Get(string key) =>
        Credentials.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}

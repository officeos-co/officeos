namespace OffceOs.Domain.Common.Services;

public sealed record ProviderDefinition(
    string Slug,
    string DisplayName,
    ApiFormat ApiFormat,
    string BaseUrl,
    string? PlatformKeyConfigName,
    IReadOnlyList<ModelDefinition> Models);

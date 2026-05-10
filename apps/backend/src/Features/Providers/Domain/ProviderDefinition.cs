namespace OffceOs.Domain.Features.Providers;

public sealed record ProviderDefinition(
    string Slug,
    string DisplayName,
    ApiFormat ApiFormat,
    string BaseUrl,
    string? PlatformKeyConfigName,
    IReadOnlyList<ModelDefinition> Models,
    bool OrganizationProfileOnly = false,
    bool RequiresPinnedModels = false);

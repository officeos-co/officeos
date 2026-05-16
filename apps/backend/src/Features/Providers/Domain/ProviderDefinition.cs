namespace OffceOs.Features.Providers.Domain;

public sealed record ProviderDefinition(
    string Slug,
    string DisplayName,
    ApiFormat ApiFormat,
    string BaseUrl,
    IReadOnlyList<ModelDefinition> Models,
    bool ManagedCloudOnly = false,
    bool RequiresPinnedModels = false);

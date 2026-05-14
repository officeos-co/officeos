namespace OffceOs.Domain.Features.Providers;

public sealed record ProviderDefinition(
    string Slug,
    string DisplayName,
    ApiFormat ApiFormat,
    string BaseUrl,
    IReadOnlyList<ModelDefinition> Models,
    bool ManagedCloudOnly = false,
    bool RequiresPinnedModels = false);

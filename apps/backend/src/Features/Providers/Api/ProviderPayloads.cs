namespace OffceOs.Api.Features.Providers;

public sealed record ModelInfoPayload(
    string Id,
    string DisplayName,
    string Provider,
    bool IsDefault);

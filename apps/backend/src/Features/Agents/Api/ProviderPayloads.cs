namespace OffceOs.Api.Features.Agents;

public sealed record ModelInfoPayload(
    string Id,
    string DisplayName,
    string Provider,
    bool IsDefault);

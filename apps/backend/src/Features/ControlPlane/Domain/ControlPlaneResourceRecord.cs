namespace OffceOs.Features.ControlPlane.Domain;

public sealed record ControlPlaneResourceRecord(
    string Kind,
    string Name,
    string Id,
    IReadOnlyDictionary<string, object?> Fields);

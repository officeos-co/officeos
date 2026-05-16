namespace OffceOs.Domain.Features.ControlPlane;

public sealed record ControlPlaneResourceDescriptor(
    string Kind,
    string Singular,
    IReadOnlyList<string> Aliases,
    string DisplayName,
    string Description,
    string Icon,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> DisplayFields);

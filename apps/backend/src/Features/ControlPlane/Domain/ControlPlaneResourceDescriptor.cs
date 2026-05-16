namespace OffceOs.Features.ControlPlane.Domain;

public sealed record ControlPlaneResourceDescriptor(
    string Kind,
    string Singular,
    IReadOnlyList<string> Aliases,
    string DisplayName,
    string Description,
    string Icon,
    IReadOnlyList<ControlPlaneResourceCapabilityRecord> Capabilities,
    IReadOnlyList<string> DisplayFields);

public abstract record ControlPlaneResourceCapabilityRecord(string Name);

public sealed record ListControlPlaneResourceCapabilityRecord() : ControlPlaneResourceCapabilityRecord("list");

public sealed record DescribeControlPlaneResourceCapabilityRecord() : ControlPlaneResourceCapabilityRecord("describe");

public sealed record DeleteControlPlaneResourceCapabilityRecord() : ControlPlaneResourceCapabilityRecord("delete");

public sealed record LogsControlPlaneResourceCapabilityRecord() : ControlPlaneResourceCapabilityRecord("logs");

public static class ControlPlaneResourceCapabilityRegistry
{
    public static readonly ListControlPlaneResourceCapabilityRecord List = new();
    public static readonly DescribeControlPlaneResourceCapabilityRecord Describe = new();
    public static readonly DeleteControlPlaneResourceCapabilityRecord Delete = new();
    public static readonly LogsControlPlaneResourceCapabilityRecord Logs = new();
}

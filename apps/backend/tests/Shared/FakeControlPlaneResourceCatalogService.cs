using OffceOs.Features.ControlPlane.Application;
using OffceOs.Features.ControlPlane.Domain;

namespace OffceOs.Tests.Shared;

public sealed class FakeControlPlaneResourceCatalogService : IControlPlaneResourceCatalogService
{
    private static readonly IReadOnlyList<ControlPlaneResourceDescriptor> Descriptors =
    [
        new("agents", "agent", ["agent"], "Agents", "Agent resources", "hubot", [ControlPlaneResourceCapabilityRegistry.Logs], ["name"]),
        new("channels", "channel", ["channel"], "Channels", "Channel resources", "broadcast", [ControlPlaneResourceCapabilityRegistry.Logs], ["name"]),
        new("control-plane", "control-plane", ["controlplane", "system"], "Control Plane", "Control plane logs", "server-process", [ControlPlaneResourceCapabilityRegistry.Logs], ["name"]),
        new("routines", "routine", ["routine"], "Routines", "Routine resources", "clock", [ControlPlaneResourceCapabilityRegistry.Logs], ["name"]),
    ];

    public IReadOnlyList<ControlPlaneResourceDescriptor> List() => Descriptors;

    public ControlPlaneResourceDescriptor? Find(string kindOrAlias) =>
        Descriptors.FirstOrDefault(descriptor =>
            string.Equals(descriptor.Kind, kindOrAlias, StringComparison.OrdinalIgnoreCase)
            || string.Equals(descriptor.Singular, kindOrAlias, StringComparison.OrdinalIgnoreCase)
            || descriptor.Aliases.Any(alias => string.Equals(alias, kindOrAlias, StringComparison.OrdinalIgnoreCase)));
}

using OffceOs.Domain.Features.ControlPlane;

namespace OffceOs.Application.Features.ControlPlane;

internal sealed class ControlPlaneResourceCatalogService : IControlPlaneResourceCatalogService
{
    private readonly IReadOnlyList<ControlPlaneResourceDescriptor> _descriptors;

    public ControlPlaneResourceCatalogService()
    {
        _descriptors = ControlPlaneResourceRegistry.Resources
            .OrderBy(descriptor => descriptor.Kind, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ControlPlaneResourceDescriptor> List() => _descriptors;

    public ControlPlaneResourceDescriptor? Find(string kindOrAlias)
    {
        return _descriptors.FirstOrDefault(descriptor =>
            string.Equals(descriptor.Kind, kindOrAlias, StringComparison.OrdinalIgnoreCase)
            || string.Equals(descriptor.Singular, kindOrAlias, StringComparison.OrdinalIgnoreCase)
            || descriptor.Aliases.Any(alias => string.Equals(alias, kindOrAlias, StringComparison.OrdinalIgnoreCase)));
    }
}

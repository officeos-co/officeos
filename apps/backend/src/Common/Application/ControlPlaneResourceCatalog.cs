namespace OffceOs.Application;

public sealed record ControlPlaneResourceDescriptor(
    string Kind,
    string Singular,
    IReadOnlyList<string> Aliases,
    string DisplayName,
    string Description,
    string Icon,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> DisplayFields);

public interface IControlPlaneResourceCatalogProvider
{
    ControlPlaneResourceDescriptor Descriptor { get; }
}

public interface IControlPlaneResourceCatalogService
{
    IReadOnlyList<ControlPlaneResourceDescriptor> List();
    ControlPlaneResourceDescriptor? Find(string kindOrAlias);
}

public sealed class StaticControlPlaneResourceCatalogProvider : IControlPlaneResourceCatalogProvider
{
    public StaticControlPlaneResourceCatalogProvider(ControlPlaneResourceDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public ControlPlaneResourceDescriptor Descriptor { get; }
}

internal sealed class ControlPlaneResourceCatalogService : IControlPlaneResourceCatalogService
{
    private readonly IReadOnlyList<ControlPlaneResourceDescriptor> _descriptors;

    public ControlPlaneResourceCatalogService(IEnumerable<IControlPlaneResourceCatalogProvider> providers)
    {
        _descriptors = providers
            .Select(provider => provider.Descriptor)
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

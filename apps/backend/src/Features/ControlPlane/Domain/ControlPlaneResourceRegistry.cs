namespace OffceOs.Domain.Features.ControlPlane;

public static class ControlPlaneResourceRegistry
{
    public static IReadOnlyList<ControlPlaneResourceDescriptor> Resources { get; } =
        DiscoverResourceDefinitions()
            .Select(definition => definition.ToDescriptor())
            .OrderBy(descriptor => descriptor.Kind, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<ControlPlaneResourceDefinition> DiscoverResourceDefinitions()
    {
        return typeof(ControlPlaneResourceDefinition)
            .Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false } && type.IsAssignableTo(typeof(ControlPlaneResourceDefinition)))
            .Select(type => (ControlPlaneResourceDefinition)Activator.CreateInstance(type)!)
            .ToArray();
    }
}

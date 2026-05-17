using OffceOs.Features.ControlPlane.Domain;

namespace OffceOs.Features.ControlPlane.Application;

internal sealed class ControlPlaneResourceService : IControlPlaneResourceService
{
    private readonly IControlPlaneResourceCatalogService _controlPlaneResourceCatalogService;
    private readonly IReadOnlyDictionary<string, IControlPlaneResourceResolver> _controlPlaneResourceResolvers;

    public ControlPlaneResourceService(
        IControlPlaneResourceCatalogService controlPlaneResourceCatalogService,
        IEnumerable<IControlPlaneResourceResolver> controlPlaneResourceResolvers)
    {
        _controlPlaneResourceCatalogService = controlPlaneResourceCatalogService;
        _controlPlaneResourceResolvers = controlPlaneResourceResolvers.ToDictionary(
            resolver => resolver.Kind,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ControlPlaneResourceDescriptor> ListDefinitions() =>
        _controlPlaneResourceCatalogService.List();

    public ControlPlaneResourceDescriptor? FindDefinition(string kindOrAlias) =>
        _controlPlaneResourceCatalogService.Find(kindOrAlias);

    public async Task<IReadOnlyList<ControlPlaneResourceRecord>?> ListAsync(
        string kindOrAlias,
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var resolver = Resolve(kindOrAlias);
        return resolver is null ? null : await resolver.ListAsync(scope, ct);
    }

    public async Task<ControlPlaneResourceRecord?> DescribeAsync(
        string kindOrAlias,
        string name,
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var resolver = Resolve(kindOrAlias);
        return resolver is null ? null : await resolver.DescribeAsync(scope, name, ct);
    }

    public async Task<ControlPlaneResourceDeleteResult> DeleteAsync(
        string kindOrAlias,
        string name,
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var descriptor = _controlPlaneResourceCatalogService.Find(kindOrAlias);
        if (descriptor is null)
            return ControlPlaneResourceDeleteResult.NotFound($"{kindOrAlias}/{name}");

        if (!_controlPlaneResourceResolvers.TryGetValue(descriptor.Kind, out var resolver))
            return ControlPlaneResourceDeleteResult.NotFound($"{descriptor.Kind}/{name}");

        if (resolver is not IDeletableControlPlaneResourceResolver deletableResolver)
            return ControlPlaneResourceDeleteResult.UnsupportedResult($"{descriptor.Kind}/{name}");

        return await deletableResolver.DeleteAsync(scope, name, ct)
            ? ControlPlaneResourceDeleteResult.DeletedResult()
            : ControlPlaneResourceDeleteResult.NotFound($"{descriptor.Kind}/{name}");
    }

    public async Task<ControlPlaneMessageResult> SendMessageAsync(
        string kindOrAlias,
        string name,
        ControlPlaneMessageRequest request,
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var descriptor = _controlPlaneResourceCatalogService.Find(kindOrAlias);
        if (descriptor is null)
            return ControlPlaneMessageResult.NotFoundResult($"{kindOrAlias}/{name}");

        if (!_controlPlaneResourceResolvers.TryGetValue(descriptor.Kind, out var resolver))
            return ControlPlaneMessageResult.NotFoundResult($"{descriptor.Kind}/{name}");

        if (resolver is not IMessageControlPlaneResourceResolver messageResolver)
            return ControlPlaneMessageResult.UnsupportedResult($"{descriptor.Kind}/{name}");

        return await messageResolver.SendMessageAsync(scope, name, request, ct);
    }

    public async Task<ControlPlaneAuthenticationResult> AuthenticateAsync(
        string kindOrAlias,
        string name,
        ControlPlaneAuthenticationRequest request,
        ControlPlaneResourceScope scope,
        CancellationToken ct = default)
    {
        var descriptor = _controlPlaneResourceCatalogService.Find(kindOrAlias);
        if (descriptor is null)
            return ControlPlaneAuthenticationResult.NotFoundResult($"{kindOrAlias}/{name}");

        if (!_controlPlaneResourceResolvers.TryGetValue(descriptor.Kind, out var resolver))
            return ControlPlaneAuthenticationResult.NotFoundResult($"{descriptor.Kind}/{name}");

        if (resolver is not IAuthenticatableControlPlaneResourceResolver authenticatableResolver)
            return ControlPlaneAuthenticationResult.UnsupportedResult($"{descriptor.Kind}/{name}");

        return await authenticatableResolver.AuthenticateAsync(scope, name, request, ct);
    }

    private IControlPlaneResourceResolver? Resolve(string kindOrAlias)
    {
        var descriptor = _controlPlaneResourceCatalogService.Find(kindOrAlias);
        return descriptor is not null && _controlPlaneResourceResolvers.TryGetValue(descriptor.Kind, out var resolver)
            ? resolver
            : null;
    }
}

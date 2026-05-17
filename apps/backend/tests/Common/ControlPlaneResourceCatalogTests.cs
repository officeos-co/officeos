using OffceOs.Features.ControlPlane.Api;
using OffceOs.Features.ControlPlane.Application;
using OffceOs.Features.ControlPlane.Domain;
using OffceOs.Features.Management.Domain;
using OffceOs.Extensions;

namespace OffceOs.Tests.Common;

public sealed class ControlPlaneResourceCatalogTests
{
    [Fact]
    public void AddApplication_registers_resource_catalog()
    {
        using var services = new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider();

        var catalog = services.GetRequiredService<IControlPlaneResourceCatalogService>();
        var resources = catalog.List();

        Assert.Contains(resources, resource => resource.Kind == "agents" && resource.Capabilities.OfType<LogsControlPlaneResourceCapabilityRecord>().Any());
        Assert.Contains(resources, resource => resource.Kind == "credentials" && resource.Aliases.Contains("credential"));
        Assert.Contains(resources, resource => resource.Kind == "models" && !resource.Capabilities.OfType<DeleteControlPlaneResourceCapabilityRecord>().Any());
        Assert.Equal("memory-stores", catalog.Find("memorystore")?.Kind);
        Assert.All(resources, resource => Assert.Contains(resource.Capabilities, capability => capability is LogsControlPlaneResourceCapabilityRecord));
    }

    [Fact]
    public void ListResources_requires_authenticated_user()
    {
        var controller = new ControlPlaneResourceCatalogController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        var result = controller.ListResources(new FakeControlPlaneResourceCatalogService());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void ListResources_returns_catalog_for_authenticated_user()
    {
        var controller = new ControlPlaneResourceCatalogController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        controller.HttpContext.Items["User"] = new UserRecord { Email = "cli@example.com" };

        var result = Assert.IsType<OkObjectResult>(controller.ListResources(new FakeControlPlaneResourceCatalogService()));
        var resources = Assert.IsAssignableFrom<IReadOnlyList<ControlPlaneResourceDescriptor>>(result.Value);

        Assert.Single(resources);
        Assert.Equal("widgets", resources[0].Kind);
    }

    private sealed class FakeControlPlaneResourceCatalogService : IControlPlaneResourceService
    {
        private static readonly ControlPlaneResourceDescriptor[] Resources =
        [
            new(
                Kind: "widgets",
                Singular: "widget",
                Aliases: ["widget"],
                DisplayName: "Widgets",
                Description: "Widget resources",
                Icon: "package",
                Capabilities: [ControlPlaneResourceCapabilityRegistry.List],
                DisplayFields: ["name"]),
        ];

        public IReadOnlyList<ControlPlaneResourceDescriptor> ListDefinitions() => Resources;

        public ControlPlaneResourceDescriptor? FindDefinition(string kindOrAlias) =>
            Resources.FirstOrDefault(resource => resource.Kind == kindOrAlias || resource.Aliases.Contains(kindOrAlias));

        public Task<IReadOnlyList<ControlPlaneResourceRecord>?> ListAsync(string kindOrAlias, ControlPlaneResourceScope scope, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ControlPlaneResourceRecord>?>([]);

        public Task<ControlPlaneResourceRecord?> DescribeAsync(string kindOrAlias, string name, ControlPlaneResourceScope scope, CancellationToken ct = default) =>
            Task.FromResult<ControlPlaneResourceRecord?>(null);

        public Task<ControlPlaneResourceDeleteResult> DeleteAsync(string kindOrAlias, string name, ControlPlaneResourceScope scope, CancellationToken ct = default) =>
            Task.FromResult(new ControlPlaneResourceDeleteResult(false, true, "unsupported"));

        public Task<ControlPlaneMessageResult> SendMessageAsync(string kindOrAlias, string name, ControlPlaneMessageRequest request, ControlPlaneResourceScope scope, CancellationToken ct = default) =>
            Task.FromResult(ControlPlaneMessageResult.UnsupportedResult("widgets"));

        public Task<ControlPlaneAuthenticationResult> AuthenticateAsync(string kindOrAlias, string name, ControlPlaneAuthenticationRequest request, ControlPlaneResourceScope scope, CancellationToken ct = default) =>
            Task.FromResult(ControlPlaneAuthenticationResult.UnsupportedResult("widgets"));
    }
}

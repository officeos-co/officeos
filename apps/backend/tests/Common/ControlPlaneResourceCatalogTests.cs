using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OffceOs.Api.Common;
using OffceOs.Application;
using OffceOs.Domain.Features.Management;
using OffceOs.Extensions;
using Xunit;

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

        Assert.Contains(resources, resource => resource.Kind == "agents" && resource.Capabilities.Contains("logs"));
        Assert.Contains(resources, resource => resource.Kind == "credentials" && resource.Aliases.Contains("credential"));
        Assert.Contains(resources, resource => resource.Kind == "models" && !resource.Capabilities.Contains("delete"));
        Assert.Equal("memory-stores", catalog.Find("memorystore")?.Kind);
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

    private sealed class FakeControlPlaneResourceCatalogService : IControlPlaneResourceCatalogService
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
                Capabilities: ["list"],
                DisplayFields: ["name"]),
        ];

        public IReadOnlyList<ControlPlaneResourceDescriptor> List() => Resources;

        public ControlPlaneResourceDescriptor? Find(string kindOrAlias) =>
            Resources.FirstOrDefault(resource => resource.Kind == kindOrAlias || resource.Aliases.Contains(kindOrAlias));
    }
}

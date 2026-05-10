using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Integrations;

public sealed class IntegrationDefinitionServiceTests
{
    [Fact]
    public async Task RegisterAsync_adds_custom_server_to_owner_catalog()
    {
        var service = IntegrationDefinitionServiceTestFactory.CreateService();

        await service.RegisterAsync(TestIds.OwnerId, TestIds.WorkspaceId, IntegrationDefinitionServiceTestFactory.CustomServer());

        var all = await service.ListAsync(TestIds.OwnerId, TestIds.WorkspaceId);
        var custom = Assert.Single(all, s => s.Name == "custom-server");
        Assert.Equal("Custom Server", custom.Title);
        Assert.False(custom.IsBuiltin);
    }

    [Fact]
    public async Task RegisterAsync_updates_existing_custom_server()
    {
        var service = IntegrationDefinitionServiceTestFactory.CreateService();

        await service.RegisterAsync(TestIds.OwnerId, TestIds.WorkspaceId, IntegrationDefinitionServiceTestFactory.CustomServer(command: "npx"));
        await service.RegisterAsync(TestIds.OwnerId, TestIds.WorkspaceId, IntegrationDefinitionServiceTestFactory.CustomServer(command: "uvx"));

        var custom = await service.GetAsync(TestIds.OwnerId, "custom-server", TestIds.WorkspaceId);
        Assert.NotNull(custom);
        Assert.Equal("uvx", custom.Command);
    }

    [Fact]
    public async Task SaveCredentialAsync_marks_custom_server_configured()
    {
        var service = IntegrationDefinitionServiceTestFactory.CreateService();

        await service.RegisterAsync(TestIds.OwnerId, TestIds.WorkspaceId, IntegrationDefinitionServiceTestFactory.CustomServer(
            credentialFieldsJson:
            """[{"name":"API_KEY","label":"API Key","type":"password","required":true}]"""));
        await service.SaveCredentialAsync(TestIds.OwnerId, TestIds.WorkspaceId, "custom-server", new() { ["API_KEY"] = "secret" });

        var custom = await service.GetAsync(TestIds.OwnerId, "custom-server", TestIds.WorkspaceId);
        Assert.NotNull(custom);
        Assert.True(custom.CredentialConfigured);
    }

    [Fact]
    public async Task DeleteAsync_removes_custom_server_and_owner_bindings()
    {
        var agents = new FakeAgentIntegrationRepository();
        var agentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var service = IntegrationDefinitionServiceTestFactory.CreateService(agentServers: agents);

        await service.RegisterAsync(TestIds.OwnerId, TestIds.WorkspaceId, IntegrationDefinitionServiceTestFactory.CustomServer());
        await service.AssignToAgentAsync(agentId, "custom-server", TestIds.OwnerId);
        await service.DeleteAsync(TestIds.OwnerId, "custom-server", TestIds.WorkspaceId);

        Assert.Null(await service.GetAsync(TestIds.OwnerId, "custom-server", TestIds.WorkspaceId));
        Assert.Empty(agents.AssignedIntegrationNames);
    }

}

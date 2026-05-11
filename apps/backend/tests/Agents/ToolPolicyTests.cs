using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class ToolPolicyTests
{
    [Fact]
    public async Task Organization_policy_filters_builtin_network_write_and_integration_tool_definitions()
    {
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var factory = ToolRegistryTestFactory.CreateFactory(new OrganizationPolicyProfileRecord
        {
            OrganizationId = Guid.NewGuid(),
            ShellToolsEnabled = false,
            NetworkToolsEnabled = false,
            FileWriteToolsEnabled = false,
            DeniedIntegrationsJson = """["salesforce"]""",
        });
        var integrations = new[]
        {
            new IntegrationDefinitionRecord
            {
                Name = "salesforce",
                Title = "Salesforce",
                Tools = [new IntegrationCatalogToolRecord("query", "Query records", new { type = "object", properties = new { } })],
            },
        };

        await using var registry = await factory.CreateAsync(new ToolRegistryRequest
        {
            Sandbox = new FakeAgentSandbox(),
            SandboxId = "sandbox",
            ServiceUrl = "http://sandbox",
            AgentId = agentId,
            WorkspaceId = workspaceId,
            OwnerId = Guid.NewGuid(),
            CorrelationId = "correlation",
            Integrations = integrations,
        }, CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("shell", toolNames);
        Assert.DoesNotContain("file_write", toolNames);
        Assert.DoesNotContain("file_edit", toolNames);
        Assert.DoesNotContain("http_request", toolNames);
        Assert.DoesNotContain("web_fetch", toolNames);
        Assert.DoesNotContain("salesforce__query", toolNames);
        Assert.Contains("file_read", toolNames);
        Assert.Contains("tool_search", toolNames);
    }

    [Fact]
    public async Task Allowed_integration_policy_exposes_only_matching_integration_tool_definitions()
    {
        var factory = ToolRegistryTestFactory.CreateFactory(new OrganizationPolicyProfileRecord
        {
            OrganizationId = Guid.NewGuid(),
            AllowedIntegrationsJson = """["google-docs"]""",
        });
        var integrations = new[]
        {
            new IntegrationDefinitionRecord
            {
                Name = "google-docs",
                Title = "Google Docs",
                Tools = [new IntegrationCatalogToolRecord("create_document", "Create a document", new { type = "object", properties = new { } })],
            },
            new IntegrationDefinitionRecord
            {
                Name = "salesforce",
                Title = "Salesforce",
                Tools = [new IntegrationCatalogToolRecord("query", "Query records", new { type = "object", properties = new { } })],
            },
        };

        await using var registry = await factory.CreateAsync(new ToolRegistryRequest
        {
            Sandbox = new FakeAgentSandbox(),
            SandboxId = "sandbox",
            ServiceUrl = "http://sandbox",
            AgentId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            CorrelationId = "correlation",
            Integrations = integrations,
        }, CancellationToken.None);

        var toolNames = registry.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("google_docs__create_document", toolNames);
        Assert.DoesNotContain("salesforce__query", toolNames);
    }

}

using OffceOs.Application.Features.Integrations;
using OffceOs.Configuration;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Infrastructure.Common.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace OffceOs.Tests.Shared;

internal static class IntegrationDefinitionServiceTestFactory
{
    public static IntegrationDefinitionService CreateService(
        FakeAgentIntegrationRepository? agentServers = null,
        FakeIntegrationDefinitionRepository? servers = null,
        FakeIntegrationCredentialRepository? credentials = null)
    {
        return new IntegrationDefinitionService(
            agentServers ?? new FakeAgentIntegrationRepository(),
            new FakeAgentRepository(),
            servers ?? new FakeIntegrationDefinitionRepository(),
            credentials ?? new FakeIntegrationCredentialRepository(),
            new FakeIntegrationCredentialEncryptionService(),
            NullLogger<IntegrationDefinitionService>.Instance,
            new FakeIntegrationDeploymentRepository(),
            new FakeWorkspaceRepository(),
            new FakeWorkspaceMemberRepository(),
            new FakeOrganizationRepository());
    }

    public static IntegrationDefinitionRecord CustomServer(
        string name = "custom-server",
        string command = "npx",
        string? credentialFieldsJson = null) => new()
    {
        Name = name,
        Title = "Custom Server",
        TransportType = IntegrationTransportType.Stdio,
        Command = command,
        Args = """["-y","custom-integration"]""",
        Category = "custom",
        CredentialFieldsJson = credentialFieldsJson,
    };
}

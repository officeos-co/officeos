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
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-test-keys-{Guid.NewGuid():N}");
        var protector = new CredentialProtector(
            DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));

        return new IntegrationDefinitionService(
            agentServers ?? new FakeAgentIntegrationRepository(),
            new FakeAgentRepository(),
            servers ?? new FakeIntegrationDefinitionRepository(),
            credentials ?? new FakeIntegrationCredentialRepository(),
            new FakeOAuthTokenRepository(),
            protector,
            new GoogleOAuthConfig(),
            NullLogger<IntegrationDefinitionService>.Instance,
            new FakeIntegrationDeploymentRepository(),
            new FakeWorkspaceRepository(),
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

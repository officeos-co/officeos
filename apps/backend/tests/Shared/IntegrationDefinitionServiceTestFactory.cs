using OffceOs.Application.Features.Integrations;
using OffceOs.Domain.Features.Management;
using OffceOs.Domain.Features.Integrations;

namespace OffceOs.Tests.Shared;

internal static class IntegrationDefinitionServiceTestFactory
{
    public static IntegrationDefinitionService CreateService(
        FakeAgentIntegrationRepository? agentServers = null,
        FakeIntegrationDefinitionRepository? servers = null,
        FakeIntegrationCredentialRepository? credentials = null)
    {
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository
            .UpsertAsync(WorkspaceMemberRecord.Create(TestIds.WorkspaceId, TestIds.OwnerId, WorkspaceRole.Owner))
            .GetAwaiter()
            .GetResult();

        return new IntegrationDefinitionService(
            agentServers ?? new FakeAgentIntegrationRepository(),
            new FakeAgentRepository(),
            servers ?? new FakeIntegrationDefinitionRepository(),
            credentials ?? new FakeIntegrationCredentialRepository(),
            new FakeIntegrationCredentialEncryptionService(),
            new FakeResourceLogWriterService(),
            new FakeIntegrationDeploymentRepository(),
            new FakeWorkspaceRepository(),
            workspaceMemberRepository);
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

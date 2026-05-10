namespace OffceOs.Api.Features.Integrations;

public sealed record IntegrationDeploymentPayload(
    Guid Id,
    Guid OrganizationId,
    Guid WorkspaceId,
    string IntegrationName,
    Guid CreatedById,
    bool Enabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DeployIntegrationInput(Guid OrganizationId, Guid WorkspaceId, string IntegrationName);

internal static class IntegrationDeploymentGraphQLMapper
{
    public static IntegrationDeploymentPayload ToPayload(IntegrationDeploymentRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.WorkspaceId,
        record.IntegrationName,
        record.CreatedById,
        record.Enabled,
        record.CreatedAt,
        record.UpdatedAt);
}

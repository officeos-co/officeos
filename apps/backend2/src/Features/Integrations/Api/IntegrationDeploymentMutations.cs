namespace OffceOs.Api.Features.Integrations;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class IntegrationDeploymentMutations
{
    public async Task<IntegrationDeploymentPayload> DeployIntegration(
        DeployIntegrationInput input,
        [Service] UserContext user,
        [Service] IIntegrationDeploymentService integrationDeploymentService,
        CancellationToken ct)
    {
        try
        {
            var deployment = await integrationDeploymentService.DeployAsync(
                user.Id,
                input.OrganizationId,
                input.WorkspaceId,
                input.IntegrationName,
                ct);
            return IntegrationDeploymentGraphQLMapper.ToPayload(deployment);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<bool> RevokeIntegrationDeployment(
        Guid organizationId,
        Guid workspaceId,
        string integrationName,
        [Service] UserContext user,
        [Service] IIntegrationDeploymentService integrationDeploymentService,
        CancellationToken ct)
    {
        return await integrationDeploymentService.RevokeAsync(user.Id, organizationId, workspaceId, integrationName, ct);
    }
}

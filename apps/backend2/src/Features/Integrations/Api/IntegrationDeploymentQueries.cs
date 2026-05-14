namespace OffceOs.Api.Features.Integrations;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class IntegrationDeploymentQueries
{
    public async Task<IReadOnlyList<IntegrationDeploymentPayload>> GetIntegrationDeployments(
        Guid organizationId,
        Guid? workspaceId,
        [Service] UserContext user,
        [Service] IIntegrationDeploymentService integrationDeploymentService,
        CancellationToken ct)
    {
        var deployments = await integrationDeploymentService.ListAsync(user.Id, organizationId, workspaceId, ct);
        return deployments.Select(IntegrationDeploymentGraphQLMapper.ToPayload).ToList();
    }
}

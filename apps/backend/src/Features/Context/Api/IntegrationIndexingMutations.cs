using EnterpriseAgentOs.Domain.Features.Context;

namespace EnterpriseAgentOs.Api.Features.Context;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class IntegrationMutations
{
    public async Task<IntegrationConnectionRecord> CreateGitHubIntegrationConnection(
        CreateGitHubIntegrationConnectionInput input,
        [Service] UserContext user,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        try
        {
            return await service.CreateGitHubConnectionAsync(new CreateGitHubIntegrationConnectionRequest(
                string.IsNullOrWhiteSpace(input.WorkspaceName) ? "default" : input.WorkspaceName,
                input.DisplayName,
                input.Repositories,
                input.Entities,
                user.Id), ct);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<IntegrationConnectionRecord> UpdateGitHubIntegrationConnection(
        UpdateGitHubIntegrationConnectionInput input,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        try
        {
            return await service.UpdateGitHubConnectionAsync(new UpdateGitHubIntegrationConnectionRequest(
                input.Id,
                input.DisplayName,
                input.Repositories,
                input.Entities), ct);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    public async Task<bool> DeleteIntegrationConnection(
        Guid id,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        await service.DeleteConnectionAsync(id, ct);
        return true;
    }

    public async Task<IntegrationIndexJobRecord> StartIntegrationIndex(
        Guid connectionId,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        try
        {
            return await service.StartIndexAsync(connectionId, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw BadInput(ex.Message);
        }
    }

    private static GraphQLException BadInput(string message)
        => new(ErrorBuilder.New().SetMessage(message).SetCode("BAD_INPUT").Build());
}

public sealed record CreateGitHubIntegrationConnectionInput(
    string WorkspaceName,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

public sealed record UpdateGitHubIntegrationConnectionInput(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

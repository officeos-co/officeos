using EnterpriseAgentOs.Domain.Features.Agents.Integrations;

namespace EnterpriseAgentOs.Api.Features.Agents.Integrations;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class AtlasMutations
{
    public async Task<IntegrationConnectionRecord> CreateAtlasGitHubConnection(
        CreateAtlasGitHubConnectionInput input,
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

    public async Task<IntegrationConnectionRecord> UpdateAtlasGitHubConnection(
        UpdateAtlasGitHubConnectionInput input,
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

    public async Task<bool> DeleteAtlasConnection(
        Guid id,
        [Service] IIntegrationConnectionService service,
        CancellationToken ct)
    {
        await service.DeleteConnectionAsync(id, ct);
        return true;
    }

    public async Task<IntegrationIndexJobRecord> StartAtlasIndex(
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

public sealed record CreateAtlasGitHubConnectionInput(
    string WorkspaceName,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

public sealed record UpdateAtlasGitHubConnectionInput(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

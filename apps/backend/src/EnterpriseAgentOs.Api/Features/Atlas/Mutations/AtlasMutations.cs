using EnterpriseAgentOs.Domain.Features.Atlas;

namespace EnterpriseAgentOs.Api.Features.Atlas;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class AtlasMutations
{
    public async Task<AtlasConnectorConnectionRecord> CreateAtlasGitHubConnection(
        CreateAtlasGitHubConnectionInput input,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            return await service.CreateGitHubConnectionAsync(new CreateAtlasGitHubConnectionRequest(
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

    public async Task<AtlasConnectorConnectionRecord> UpdateAtlasGitHubConnection(
        UpdateAtlasGitHubConnectionInput input,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            return await service.UpdateGitHubConnectionAsync(new UpdateAtlasGitHubConnectionRequest(
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
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await service.DeleteConnectionAsync(id, ct);
        return true;
    }

    public async Task<AtlasIndexJobRecord> StartAtlasIndex(
        Guid connectionId,
        IResolverContext context,
        [Service] IAtlasService service,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
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

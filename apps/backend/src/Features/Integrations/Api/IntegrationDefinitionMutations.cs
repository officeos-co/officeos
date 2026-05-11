namespace OffceOs.Api.Features.Integrations;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class IntegrationDefinitionMutations
{
    public async Task<IntegrationDefinitionRecord> RegisterIntegration(
        RegisterIntegrationInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var transportType = Enum.TryParse<IntegrationTransportType>(input.TransportType, true, out var t)
            ? t
            : IntegrationTransportType.Stdio;

        if (transportType == IntegrationTransportType.Stdio && string.IsNullOrWhiteSpace(input.Command))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Stdio integrations require a command.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        if (!string.IsNullOrWhiteSpace(input.Args))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(input.Args);
                if (parsed is null)
                    throw new JsonException("Args must be an array.");
            }
            catch (JsonException)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Integration args must be a JSON string array.")
                        .SetCode("BAD_INPUT")
                        .Build());
            }
        }

        var record = new IntegrationDefinitionRecord
        {
            Name = input.Name,
            Title = input.Title,
            Description = input.Description,
            Subtitle = input.Subtitle,
            AuthorName = input.AuthorName,
            AuthorUrl = input.AuthorUrl,
            DocumentationUrl = input.DocumentationUrl,
            RepositoryUrl = input.RepositoryUrl,
            Tools = input.Tools?.Select(tool => new IntegrationCatalogToolRecord(
                tool.Name,
                string.IsNullOrWhiteSpace(tool.Description) ? tool.Name : tool.Description,
                tool.Parameters)).ToList() ?? [],
            TransportType = transportType,
            Command = input.Command,
            Args = input.Args,
            Url = input.Url,
            Logo = input.Logo,
            Category = input.Category,
            CredentialFieldsJson = input.CredentialFieldsJson,
        };

        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            return await svc.RegisterAsync(user.Id, workspace.Id, record, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<bool> DeleteIntegration(
        string name,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            await svc.DeleteAsync(user.Id, name, workspace.Id, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
        return true;
    }

    public Task<bool> AssignIntegrationToAgent(
        Guid agentId, string integrationName,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        _ = svc;
        _ = ct;
        throw ImmutableAgentCapabilitiesError();
    }

    public Task<bool> UnassignIntegrationFromAgent(
        Guid agentId, string integrationName,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        _ = svc;
        _ = ct;
        throw ImmutableAgentCapabilitiesError();
    }

    private static GraphQLException ImmutableAgentCapabilitiesError() =>
        new(ErrorBuilder.New()
            .SetMessage("Agent integrations are immutable after agent creation. Create a new agent with the desired integrations.")
            .SetCode("IMMUTABLE_AGENT_CAPABILITIES")
            .Build());

    public async Task<bool> SaveIntegrationCredential(
        string integrationName, List<CredentialFieldInput> fields,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var dict = fields.ToDictionary(f => f.Key, f => f.Value);
        await svc.SaveCredentialAsync(user.Id, workspace.Id, integrationName, dict, ct);
        return true;
    }

    public async Task<bool> DisconnectIntegration(
        string integrationName,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IIntegrationDefinitionService svc, CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        await svc.ArchiveCredentialAsync(user.Id, workspace.Id, integrationName, ct);
        return true;
    }
}

public record RegisterIntegrationInput(
    string Name,
    string Title,
    string Description,
    string Subtitle,
    string AuthorName,
    string AuthorUrl,
    string DocumentationUrl,
    string RepositoryUrl,
    List<IntegrationCatalogToolInput>? Tools,
    string TransportType,
    string? Command,
    string? Args,
    string? Url,
    string Category,
    string? CredentialFieldsJson,
    string? Logo = null);

public sealed record IntegrationCatalogToolInput(
    string Name,
    string? Description,
    [property: GraphQLType(typeof(AnyType))] JsonElement? Parameters = null);

public record CredentialFieldInput(string Key, string Value);

using EnterpriseAgentOs.Api.Common;
using EnterpriseAgentOs.Domain.Features.Mcp;

namespace EnterpriseAgentOs.Api.Features.Mcp;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class McpMutations
{
    public async Task<McpServerRecord> RegisterMcpServer(
        RegisterMcpServerInput input,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        var transportType = Enum.TryParse<McpTransportType>(input.TransportType, true, out var t)
            ? t
            : McpTransportType.Stdio;

        if (transportType == McpTransportType.Stdio && string.IsNullOrWhiteSpace(input.Command))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Stdio MCP servers require a command.")
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
                        .SetMessage("MCP server args must be a JSON string array.")
                        .SetCode("BAD_INPUT")
                        .Build());
            }
        }

        var record = new McpServerRecord
        {
            Name = input.Name,
            Title = input.Title,
            Description = input.Description,
            Subtitle = input.Subtitle,
            AuthorName = input.AuthorName,
            AuthorUrl = input.AuthorUrl,
            DocumentationUrl = input.DocumentationUrl,
            RepositoryUrl = input.RepositoryUrl,
            ToolsJson = input.ToolsJson,
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
            return await svc.RegisterAsync(record, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<bool> DeleteMcpServer(
        string name,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        try
        {
            await svc.DeleteAsync(name, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
        return true;
    }

    public Task<bool> AssignMcpServerToAgent(
        Guid agentId, string serverName,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        _ = svc;
        _ = ct;
        throw ImmutableAgentCapabilitiesError();
    }

    public Task<bool> UnassignMcpServerFromAgent(
        Guid agentId, string serverName,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        _ = svc;
        _ = ct;
        throw ImmutableAgentCapabilitiesError();
    }

    private static GraphQLException ImmutableAgentCapabilitiesError() =>
        new(ErrorBuilder.New()
            .SetMessage("Agent MCP servers are immutable after agent creation. Create a new agent with the desired MCP servers.")
            .SetCode("IMMUTABLE_AGENT_CAPABILITIES")
            .Build());

    public async Task<bool> SaveMcpCredential(
        string serverName, List<CredentialFieldInput> fields,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        var dict = fields.ToDictionary(f => f.Key, f => f.Value);
        await svc.SaveCredentialAsync(serverName, dict, ct);
        return true;
    }
}

public record RegisterMcpServerInput(
    string Name,
    string Title,
    string Description,
    string Subtitle,
    string AuthorName,
    string AuthorUrl,
    string DocumentationUrl,
    string RepositoryUrl,
    string? ToolsJson,
    string TransportType,
    string? Command,
    string? Args,
    string? Url,
    string Category,
    string? CredentialFieldsJson,
    string? Logo = null);

public record CredentialFieldInput(string Key, string Value);

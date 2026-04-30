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
        var record = new McpServerRecord
        {
            Name = input.Name,
            Title = input.Title,
            Description = input.Description,
            TransportType = Enum.TryParse<McpTransportType>(input.TransportType, true, out var t) ? t : McpTransportType.Stdio,
            Command = input.Command,
            Args = input.Args,
            Url = input.Url,
            Category = input.Category,
            CredentialFieldsJson = input.CredentialFieldsJson,
        };
        return await svc.RegisterAsync(record, ct);
    }

    public async Task<bool> DeleteMcpServer(
        string name,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        await svc.DeleteAsync(name, ct);
        return true;
    }

    public async Task<bool> AssignMcpServerToAgent(
        Guid agentId, string serverName,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        await svc.AssignToAgentAsync(agentId, serverName, ct);
        return true;
    }

    public async Task<bool> UnassignMcpServerFromAgent(
        Guid agentId, string serverName,
        [Service] IMcpServerService svc, CancellationToken ct)
    {
        await svc.UnassignFromAgentAsync(agentId, serverName, ct);
        return true;
    }

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
    string? Description,
    string TransportType,
    string? Command,
    string? Args,
    string? Url,
    string? Category,
    string? CredentialFieldsJson);

public record CredentialFieldInput(string Key, string Value);

using EnterpriseAgentOs.Api.Common;
using EnterpriseAgentOs.Domain.Features.Mcp;

namespace EnterpriseAgentOs.Api.Features.Mcp;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class McpQueries
{
    public async Task<IReadOnlyList<McpServerRecord>> GetMcpServers(
        [Service] IMcpServerService svc, CancellationToken ct)
        => await svc.ListAsync(ct);

    public async Task<McpServerRecord?> GetMcpServer(
        string name,
        [Service] IMcpServerService svc, CancellationToken ct)
        => await svc.GetAsync(name, ct);

    public async Task<IReadOnlyList<McpServerRecord>> GetAgentMcpServers(
        Guid agentId,
        [Service] IMcpServerService svc, CancellationToken ct)
        => await svc.ListForAgentAsync(agentId, ct);
}

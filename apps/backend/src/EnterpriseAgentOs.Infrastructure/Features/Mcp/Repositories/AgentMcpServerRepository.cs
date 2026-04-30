using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class AgentMcpServerRepository : IAgentMcpServerRepository
{
    private readonly EaosDbContext _db;

    public AgentMcpServerRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<McpServerRecord>> ListServersForAgentAsync(Guid agentId, CancellationToken ct)
    {
        var serverNames = await _db.AgentMcpServers.AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.McpServerName)
            .ToListAsync(ct);

        if (serverNames.Count == 0) return [];

        var entities = await _db.McpServers.AsNoTracking()
            .Where(s => serverNames.Contains(s.Name))
            .ToListAsync(ct);

        return entities.Select(e => new McpServerRecord
        {
            Id = e.Id,
            Name = e.Name,
            Title = e.Title,
            Description = e.Description,
            TransportType = Enum.TryParse<McpTransportType>(e.TransportType, out var t) ? t : McpTransportType.Stdio,
            Command = e.Command,
            Args = e.Args,
            Url = e.Url,
            Logo = e.Logo,
            Category = e.Category,
            CredentialFieldsJson = e.CredentialFieldsJson,
            IsBuiltin = e.IsBuiltin,
            CreatedAt = e.CreatedAt,
        }).ToList();
    }

    public async Task AssignAsync(Guid agentId, string mcpServerName, CancellationToken ct)
    {
        var exists = await _db.AgentMcpServers.AnyAsync(
            a => a.AgentId == agentId && a.McpServerName == mcpServerName, ct);
        if (exists) return;

        _db.AgentMcpServers.Add(new AgentMcpServerEntity
        {
            AgentId = agentId,
            McpServerName = mcpServerName,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(Guid agentId, string mcpServerName, CancellationToken ct)
    {
        await _db.AgentMcpServers
            .Where(a => a.AgentId == agentId && a.McpServerName == mcpServerName)
            .ExecuteDeleteAsync(ct);
    }
}

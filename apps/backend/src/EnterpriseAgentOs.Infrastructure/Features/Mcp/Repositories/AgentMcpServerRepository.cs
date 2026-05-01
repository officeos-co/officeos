using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class AgentMcpServerRepository : IAgentMcpServerRepository
{
    private readonly EaosDbContext _db;

    public AgentMcpServerRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> ListServerNamesForAgentAsync(Guid agentId, CancellationToken ct)
    {
        return await _db.AgentMcpServers.AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.McpServerName)
            .ToListAsync(ct);
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

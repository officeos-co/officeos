using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Agents.Integrations;

internal sealed class AgentIntegrationDefinitionRepository : IAgentIntegrationDefinitionRepository
{
    private readonly EaosDbContext _db;

    public AgentIntegrationDefinitionRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct)
    {
        return await _db.AgentMcpServers.AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.IntegrationName)
            .ToListAsync(ct);
    }

    public async Task AssignAsync(Guid agentId, string mcpIntegrationName, CancellationToken ct)
    {
        var exists = await _db.AgentMcpServers.AnyAsync(
            a => a.AgentId == agentId && a.IntegrationName == mcpIntegrationName, ct);
        if (exists) return;

        _db.AgentMcpServers.Add(new AgentIntegrationDefinitionEntity
        {
            AgentId = agentId,
            IntegrationName = mcpIntegrationName,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(Guid agentId, string mcpIntegrationName, CancellationToken ct)
    {
        await _db.AgentMcpServers
            .Where(a => a.AgentId == agentId && a.IntegrationName == mcpIntegrationName)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UnassignServerFromAllAgentsAsync(string mcpIntegrationName, CancellationToken ct)
    {
        await _db.AgentMcpServers
            .Where(a => a.IntegrationName == mcpIntegrationName)
            .ExecuteDeleteAsync(ct);
    }
}

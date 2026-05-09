using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Integrations;

internal sealed class AgentIntegrationRepository : IAgentIntegrationRepository
{
    private readonly EaosDbContext _db;

    public AgentIntegrationRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct)
    {
        return await _db.AgentIntegrations.AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.IntegrationName)
            .ToListAsync(ct);
    }

    public async Task AssignAsync(Guid agentId, string integrationName, CancellationToken ct)
    {
        var exists = await _db.AgentIntegrations.AnyAsync(
            a => a.AgentId == agentId && a.IntegrationName == integrationName, ct);
        if (exists) return;

        _db.AgentIntegrations.Add(new AgentIntegrationEntity
        {
            AgentId = agentId,
            IntegrationName = integrationName,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(Guid agentId, string integrationName, CancellationToken ct)
    {
        await _db.AgentIntegrations
            .Where(a => a.AgentId == agentId && a.IntegrationName == integrationName)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UnassignIntegrationFromOwnerAgentsAsync(Guid ownerId, string integrationName, CancellationToken ct)
    {
        await _db.AgentIntegrations
            .Where(a => a.IntegrationName == integrationName
                && _db.Agents.Any(agent => agent.Id == a.AgentId && agent.OwnerId == ownerId))
            .ExecuteDeleteAsync(ct);
    }
}

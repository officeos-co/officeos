using OffceOs.Database;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Database.Models;
namespace OffceOs.Infrastructure.Features.Integrations;

internal sealed class AgentIntegrationRepository : IAgentIntegrationRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentIntegrationRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct)
    {
        return await _eaosDbContext.AgentIntegrations.AsNoTracking()
            .Where(a => a.AgentId == agentId)
            .Select(a => a.IntegrationName)
            .ToListAsync(ct);
    }

    public async Task AssignAsync(Guid agentId, string integrationName, CancellationToken ct)
    {
        var exists = await _eaosDbContext.AgentIntegrations.AnyAsync(
            a => a.AgentId == agentId && a.IntegrationName == integrationName, ct);
        if (exists) return;

        _eaosDbContext.AgentIntegrations.Add(new AgentIntegrationEntity
        {
            AgentId = agentId,
            IntegrationName = integrationName,
        });
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(Guid agentId, string integrationName, CancellationToken ct)
    {
        await _eaosDbContext.AgentIntegrations
            .Where(a => a.AgentId == agentId && a.IntegrationName == integrationName)
            .ExecuteDeleteAsync(ct);
    }

    public async Task UnassignIntegrationFromOwnerAgentsAsync(Guid ownerId, string integrationName, CancellationToken ct)
    {
        await _eaosDbContext.AgentIntegrations
            .Where(a => a.IntegrationName == integrationName
                && _eaosDbContext.Agents.Any(agent => agent.Id == a.AgentId && agent.OwnerId == ownerId))
            .ExecuteDeleteAsync(ct);
    }
}

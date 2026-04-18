namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentSkillsQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.AgentSkillDto>> GetAgentSkills(
        Guid agentId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository agentSkills,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var names = await agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        if (names.Count == 0) return Array.Empty<EnterpriseAgentOs.Api.GraphQL.Types.AgentSkillDto>();

        var perms = await db.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);

        var grouped = perms
            .GroupBy(p => p.SkillName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return names.Select(name =>
        {
            grouped.TryGetValue(name, out var rows);
            var mapped = (rows ?? new List<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord>())
                .Select(r => new EnterpriseAgentOs.Api.GraphQL.Types.AgentToolPermissionDto(r.SkillName, r.ToolName, r.Permission))
                .ToList();
            return new EnterpriseAgentOs.Api.GraphQL.Types.AgentSkillDto(name, mapped);
        }).ToList();
    }
}

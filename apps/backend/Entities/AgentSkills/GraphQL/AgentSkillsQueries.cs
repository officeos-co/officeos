using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.AgentSkills.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentSkillsQueries
{
    public async Task<IReadOnlyList<AgentSkillDto>> GetAgentSkills(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var names = await agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        if (names.Count == 0) return Array.Empty<AgentSkillDto>();

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
            var mapped = (rows ?? new List<AgentToolPermissionRecord>())
                .Select(r => new AgentToolPermissionDto(r.SkillName, r.ToolName, r.Permission))
                .ToList();
            return new AgentSkillDto(name, mapped);
        }).ToList();
    }
}

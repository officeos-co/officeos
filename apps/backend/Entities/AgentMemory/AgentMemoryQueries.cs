namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentMemoryQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGqlDto>> GetAgentMemories(
        Guid agentId,
        string? category,
        string? @namespace,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Database.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var q = db.AgentMemories.AsNoTracking().Where(m => m.AgentId == agentId);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(m => m.Category == category);
        if (!string.IsNullOrWhiteSpace(@namespace))
            q = q.Where(m => m.Namespace == @namespace);
        var rows = await q.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGraphQLMapper.ToDto).ToList();
    }

}

namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SkillQueries
{
    [GraphQLName("skills")]
    public async Task<IReadOnlyList<Types.SkillDashboardDto>> GetSkillList(
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var records = await catalog.ListAsync(ct);
        return records.Select(Types.SkillDashboardMapper.ToDto).ToList();
    }

    [GraphQLName("skill")]
    public async Task<Types.SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        return record is null ? null : Types.SkillDashboardMapper.ToDto(record);
    }

    [GraphQLName("skillComments")]
    public async Task<IReadOnlyList<Types.SkillCommentDto>> GetSkillComments(
        Guid skillId,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await db.SkillComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.SkillId == skillId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(Types.SkillDashboardMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<Types.AgentSkillDto>> GetAgentSkills(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var names = await agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        if (names.Count == 0) return Array.Empty<Types.AgentSkillDto>();

        var perms = await db.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == agentId)
            .ToListAsync(ct);

        var grouped = perms
            .GroupBy(p => p.SkillName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return names.Select(name =>
        {
            grouped.TryGetValue(name, out var rows2);
            var mapped = (rows2 ?? new List<AgentToolPermissionRecord>())
                .Select(r => new Types.AgentToolPermissionDto(r.SkillName, r.ToolName, r.Permission))
                .ToList();
            return new Types.AgentSkillDto(name, mapped);
        }).ToList();
    }
}

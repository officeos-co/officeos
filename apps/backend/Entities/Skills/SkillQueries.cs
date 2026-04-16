namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class SkillQueries
{
    [GraphQLName("skills")]
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Skills.Types.SkillDashboardDto>> GetSkillList(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Skills.ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var records = await catalog.ListAsync(ct);
        return records.Select(EnterpriseAgentOs.Api.Entities.Skills.Types.SkillDashboardMapper.ToDto).ToList();
    }

    [GraphQLName("skill")]
    public async Task<EnterpriseAgentOs.Api.Entities.Skills.Types.SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Skills.ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        return record is null ? null : EnterpriseAgentOs.Api.Entities.Skills.Types.SkillDashboardMapper.ToDto(record);
    }

    [GraphQLName("skillComments")]
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Skills.Types.SkillCommentDto>> GetSkillComments(
        Guid skillId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Database.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await db.SkillComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.SkillId == skillId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.Skills.Types.SkillDashboardMapper.ToDto).ToList();
    }
}

using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SkillQueries
{
    [GraphQLName("skills")]
    public async Task<IReadOnlyList<SkillDashboardDto>> GetSkillList(
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var records = await catalog.ListAsync(ct);
        return records.Select(SkillDashboardMapper.ToDto).ToList();
    }

    [GraphQLName("skill")]
    public async Task<SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        return record is null ? null : SkillDashboardMapper.ToDto(record);
    }

    [GraphQLName("skillComments")]
    public async Task<IReadOnlyList<SkillCommentDto>> GetSkillComments(
        Guid skillId,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await db.SkillComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.SkillId == skillId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(SkillDashboardMapper.ToDto).ToList();
    }
}

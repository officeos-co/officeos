namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SkillQueries
{
    [GraphQLName("skills")]
    public async Task<IReadOnlyList<Types.SkillDashboardDto>> GetSkillList(
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] ISkillRepository credentials,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var records = await catalog.ListAsync(ct);
        var dtos = records.Select(Types.SkillDashboardMapper.ToDto).ToList();

        var skillIds = dtos.Select(d => d.Id).ToList();

        // Batch: likes counts
        var likesCounts = await db.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId))
            .GroupBy(l => l.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        // Batch: liked by current user
        var likedByMe = (await db.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId) && l.UserId == user.Id)
            .Select(l => l.SkillId)
            .ToListAsync(ct))
            .ToHashSet();

        // Batch: comment counts
        var commentCounts = await db.SkillComments
            .Where(c => skillIds.Contains(c.SkillId))
            .GroupBy(c => c.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        // Batch: installed status
        var installedNames = (await credentials.ListAsync(ct))
            .Where(r => r.Enabled)
            .Select(r => r.SkillName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return dtos.Select(d => d with
        {
            LikesCount = likesCounts.GetValueOrDefault(d.Id),
            IsLikedByMe = likedByMe.Contains(d.Id),
            CommentCount = commentCounts.GetValueOrDefault(d.Id),
            IsInstalled = installedNames.Contains(d.Name),
        }).ToList();
    }

    [GraphQLName("skill")]
    public async Task<Types.SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] ISkillRepository credentials,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        if (record is null) return null;

        var dto = Types.SkillDashboardMapper.ToDto(record);
        var credRow = await credentials.GetByNameAsync(name, ct);

        return dto with
        {
            LikesCount = await db.SkillLikes.CountAsync(l => l.SkillId == dto.Id, ct),
            IsLikedByMe = await db.SkillLikes.AnyAsync(l => l.SkillId == dto.Id && l.UserId == user.Id, ct),
            CommentCount = await db.SkillComments.CountAsync(c => c.SkillId == dto.Id, ct),
            IsInstalled = credRow?.Enabled == true,
        };
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

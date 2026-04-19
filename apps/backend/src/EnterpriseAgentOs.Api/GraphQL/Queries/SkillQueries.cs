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
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var records = await catalog.ListAsync(ct);
        var dtos = records.Select(Types.SkillDashboardMapper.ToDto).ToList();
        var skillIds = dtos.Select(d => d.Id).ToList();

        var likesCounts = await catalog.BatchLikesCountAsync(skillIds, ct);
        var likedByMe = await catalog.BatchLikedByUserAsync(skillIds, user.Id, ct);
        var commentCounts = await catalog.BatchCommentCountAsync(skillIds, ct);
        var installedNames = await catalog.BatchInstalledNamesAsync(ct);
        var configuredNames = await catalog.BatchConfiguredNamesAsync(ct);

        return dtos.Select(d => d with
        {
            LikesCount = likesCounts.GetValueOrDefault(d.Id),
            IsLikedByMe = likedByMe.Contains(d.Id),
            CommentCount = commentCounts.GetValueOrDefault(d.Id),
            IsInstalled = installedNames.Contains(d.Name),
            IsConfigured = configuredNames.Contains(d.Name),
        }).ToList();
    }

    [GraphQLName("skill")]
    public async Task<Types.SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] ISkillRepository credentials,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        if (record is null) return null;

        var dto = Types.SkillDashboardMapper.ToDto(record);
        var ids = new List<Guid> { dto.Id };
        var credRow = await credentials.GetByNameAsync(name, ct);
        var likesCounts = await catalog.BatchLikesCountAsync(ids, ct);
        var likedByMe = await catalog.BatchLikedByUserAsync(ids, user.Id, ct);
        var commentCounts = await catalog.BatchCommentCountAsync(ids, ct);

        return dto with
        {
            LikesCount = likesCounts.GetValueOrDefault(dto.Id),
            IsLikedByMe = likedByMe.Contains(dto.Id),
            CommentCount = commentCounts.GetValueOrDefault(dto.Id),
            IsInstalled = credRow?.Enabled == true,
            IsConfigured = credRow?.Enabled == true && credRow?.EncryptedCredentials != null,
        };
    }

    [GraphQLName("skillComments")]
    public async Task<IReadOnlyList<Types.SkillCommentDto>> GetSkillComments(
        Guid skillId,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await catalog.ListCommentsBySkillAsync(skillId, ct);
        return rows.Select(Types.SkillDashboardMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<Types.AgentSkillDto>> GetAgentSkills(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var names = await agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        if (names.Count == 0) return Array.Empty<Types.AgentSkillDto>();

        var perms = await agentSkills.ListToolPermissionsAsync(agentId, ct);
        var grouped = perms
            .GroupBy(p => p.SkillName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return names.Select(name =>
        {
            grouped.TryGetValue(name, out var rows);
            var mapped = (rows ?? new List<AgentToolPermissionRecord>())
                .Select(r => new Types.AgentToolPermissionDto(r.SkillName, r.ToolName, r.Permission))
                .ToList();
            return new Types.AgentSkillDto(name, mapped);
        }).ToList();
    }
}

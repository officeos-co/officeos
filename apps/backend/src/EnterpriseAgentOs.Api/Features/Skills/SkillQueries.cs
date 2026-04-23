namespace EnterpriseAgentOs.Api.Features.Skills;

[ExtendObjectType(typeof(GraphQLQueries))]
public class SkillQueries
{
    private static readonly TimeSpan SkillListCacheTtl = TimeSpan.FromMinutes(2);
    private const string SkillListCacheKey = "skills:dashboard:list";

    [GraphQLName("skills")]
    public async Task<IReadOnlyList<SkillDashboardDto>> GetSkillList(
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var cacheKey = $"{SkillListCacheKey}:{user.Id}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<SkillDashboardDto>? cached) && cached is not null)
            return cached;

        var records = await catalog.ListAsync(ct);
        var dtos = records.Select(SkillDashboardMapper.ToDto).ToList();
        var skillIds = dtos.Select(d => d.Id).ToList();

        var likesCounts = await catalog.BatchLikesCountAsync(skillIds, ct);
        var likedByMe = await catalog.BatchLikedByUserAsync(skillIds, user.Id, ct);
        var commentCounts = await catalog.BatchCommentCountAsync(skillIds, ct);
        var installedNames = await catalog.BatchInstalledNamesAsync(ct);
        var configuredNames = await catalog.BatchConfiguredNamesAsync(ct);

        var result = dtos.Select(d => d with
        {
            LikesCount = likesCounts.GetValueOrDefault(d.Id),
            IsLikedByMe = likedByMe.Contains(d.Id),
            CommentCount = commentCounts.GetValueOrDefault(d.Id),
            IsInstalled = installedNames.Contains(d.Name),
            IsConfigured = configuredNames.Contains(d.Name),
        }).ToList();

        cache.Set(cacheKey, (IReadOnlyList<SkillDashboardDto>)result,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = SkillListCacheTtl });
        return result;
    }

    [GraphQLName("skill")]
    public async Task<SkillDashboardDto?> GetSkillByName(
        string name,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] ISkillRepository credentials,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var record = await catalog.GetByNameAsync(name, ct);
        if (record is null) return null;

        var dto = SkillDashboardMapper.ToDto(record);
        var ids = new List<Guid> { dto.Id };
        var credRow = await credentials.GetByNameAsync(name, ct);
        var likesCounts = await catalog.BatchLikesCountAsync(ids, ct);
        var likedByMe = await catalog.BatchLikedByUserAsync(ids, user.Id, ct);
        var commentCounts = await catalog.BatchCommentCountAsync(ids, ct);

        var configuredNames = await catalog.BatchConfiguredNamesAsync(ct);

        return dto with
        {
            LikesCount = likesCounts.GetValueOrDefault(dto.Id),
            IsLikedByMe = likedByMe.Contains(dto.Id),
            CommentCount = commentCounts.GetValueOrDefault(dto.Id),
            IsInstalled = credRow?.Enabled == true,
            IsConfigured = configuredNames.Contains(name),
        };
    }

    [GraphQLName("skillComments")]
    public async Task<IReadOnlyList<SkillCommentDto>> GetSkillComments(
        Guid skillId,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var rows = await catalog.ListCommentsBySkillAsync(skillId, ct);
        return rows.Select(SkillDashboardMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<AgentSkillGqlDto>> GetAgentSkills(
        Guid agentId,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var names = await agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        if (names.Count == 0) return Array.Empty<AgentSkillGqlDto>();

        var perms = await agentSkills.ListToolPermissionsAsync(agentId, ct);
        var grouped = perms
            .GroupBy(p => p.SkillName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return names.Select(name =>
        {
            grouped.TryGetValue(name, out var rows);
            return new AgentSkillGqlDto(name, (IReadOnlyList<AgentToolPermissionRecord>)(rows ?? new List<AgentToolPermissionRecord>()));
        }).ToList();
    }

    [GraphQLName("oauthConnectionStatus")]
    public async Task<OAuthConnectionStatusDto> GetOAuthConnectionStatus(
        string provider,
        string[]? requiredScopes,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var token = await db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (token is null)
            return new OAuthConnectionStatusDto(false, null, null, false, []);

        var missing = token.MissingScopes(requiredScopes ?? []);
        var scopeString = string.Join(' ', token.GetScopeSet());

        return new OAuthConnectionStatusDto(true, token.Email, scopeString, missing.Count > 0, missing.ToArray());
    }
}

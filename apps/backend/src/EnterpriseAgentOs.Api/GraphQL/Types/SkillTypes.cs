namespace EnterpriseAgentOs.Api.GraphQL.Types;

[GraphQLName("Skill")]
public sealed record SkillDashboardDto(
    Guid Id, string Name, string Title, string Description,
    string? Doc, string Status, string Version, bool RequiresApproval,
    DateTime CreatedAt, DateTime UpdatedAt);

[GraphQLName("SkillTool")]
public sealed record SkillToolDto(string Name, string Description);

[GraphQLName("SkillAuthor")]
public sealed record SkillAuthorDto(string Name, string? Url);

[GraphQLName("SkillContributor")]
public sealed record SkillContributorDto(string Name, string? Url);

public sealed record SkillCredentialEntry(string Key, string Value);

[GraphQLName("CommentAuthor")]
public sealed record CommentAuthorDto(Guid Id, string? Name, string? AvatarUrl);

[GraphQLName("SkillComment")]
public sealed record SkillCommentDto(
    Guid Id,
    Guid SkillId,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CommentAuthorDto Author);

/// <summary>
/// Computed fields on <see cref="SkillDashboardDto"/> — likes, likedByMe,
/// commentsCount, tools, and marketplace metadata from typed columns.
/// </summary>
[ExtendObjectType(typeof(SkillDashboardDto))]
public class SkillDashboardResolvers
{
    public async Task<int> GetLikes(
        [Parent] SkillDashboardDto skill,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        return await db.SkillLikes.CountAsync(l => l.SkillId == skill.Id, ct);
    }

    public async Task<bool> GetLikedByMe(
        [Parent] SkillDashboardDto skill,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await db.SkillLikes.AnyAsync(l => l.SkillId == skill.Id && l.UserId == user.Id, ct);
    }

    public async Task<int> GetCommentsCount(
        [Parent] SkillDashboardDto skill,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        return await db.SkillComments.CountAsync(c => c.SkillId == skill.Id, ct);
    }

    public async Task<string?> GetLogo(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Logo;
    }

    public async Task<bool> GetInstalled(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillRepository repo,
        CancellationToken ct)
    {
        var row = await repo.GetByNameAsync(skill.Name, ct);
        return row?.Enabled == true;
    }

    public string GetSourceCodeUrl([Parent] SkillDashboardDto skill)
        => $"https://github.com/officeos-co/skill-{skill.Name}";

    public async Task<IReadOnlyList<SkillToolDto>> GetTools(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        if (record is null) return Array.Empty<SkillToolDto>();
        var actions = EnterpriseAgentOs.Application.Services.Skills.SkillService.DeserializeActions(record);
        return actions.Select(kv => new SkillToolDto(kv.Key, kv.Value.Description)).ToList();
    }

    public async Task<string?> GetLicense(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.License;
    }

    public async Task<string?> GetRepository(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Repository;
    }

    public async Task<IReadOnlyList<string>> GetCategories(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Categories ?? Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetKeywords(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Keywords ?? Array.Empty<string>();
    }

    public async Task<string?> GetReadme(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Readme;
    }

    public async Task<string?> GetChangelog(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        return record?.Changelog;
    }

    public async Task<SkillAuthorDto?> GetAuthor(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        if (record?.AuthorName is null) return null;
        return new SkillAuthorDto(record.AuthorName, record.AuthorUrl);
    }

    public async Task<IReadOnlyList<SkillContributorDto>> GetContributors(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        if (record is null || string.IsNullOrWhiteSpace(record.ContributorsJson))
            return Array.Empty<SkillContributorDto>();
        var contributors = JsonSerializer.Deserialize<ManifestContributor[]>(record.ContributorsJson,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });
        if (contributors is null) return Array.Empty<SkillContributorDto>();
        return contributors.Select(c => new SkillContributorDto(c.Name, c.Url)).ToList();
    }
}

internal static class SkillDashboardMapper
{
    public static SkillDashboardDto ToDto(SkillRecord r) =>
        new(r.Id, r.Name, r.Title, r.Description, r.Doc,
            r.Status, r.Version, r.RequiresApproval, r.CreatedAt, r.UpdatedAt);

    public static SkillCommentDto ToDto(SkillCommentRecord c) =>
        new(c.Id, c.SkillId, c.Body, c.CreatedAt, c.UpdatedAt,
            new CommentAuthorDto(
                c.User?.Id ?? c.UserId,
                c.User?.Name,
                c.User?.AvatarUrl));
}

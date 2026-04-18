namespace EnterpriseAgentOs.Api.GraphQL.Types;

[GraphQLName("Skill")]
public sealed record SkillDashboardDto(
    Guid Id, string Name, string Title, string Description,
    string? Doc, string Status, string Version,
    DateTime CreatedAt, DateTime UpdatedAt);

[GraphQLName("SkillTool")]
public sealed record SkillToolDto(string Name, string Description);

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
/// commentsCount, and tools parsed from ManifestJson.
/// </summary>
[ExtendObjectType(typeof(SkillDashboardDto))]
public class SkillDashboardResolvers
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

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
        if (record is null || string.IsNullOrWhiteSpace(record.ManifestJson)) return null;
        try
        {
            var manifest = JsonSerializer.Deserialize<RuntimeManifest>(record.ManifestJson, ManifestJsonOptions);
            return manifest?.Logo;
        }
        catch { return null; }
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
        => $"https://github.com/officeos/integrations/tree/main/packages/{skill.Name}";

    public async Task<IReadOnlyList<SkillToolDto>> GetTools(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        if (record is null || string.IsNullOrWhiteSpace(record.ManifestJson)) return Array.Empty<SkillToolDto>();
        try
        {
            var manifest = JsonSerializer.Deserialize<RuntimeManifest>(record.ManifestJson, ManifestJsonOptions);
            if (manifest is null) return Array.Empty<SkillToolDto>();
            return manifest.Actions
                .Select(kv => new SkillToolDto(kv.Key, kv.Value.Description))
                .ToList();
        }
        catch
        {
            return Array.Empty<SkillToolDto>();
        }
    }
}

internal static class SkillDashboardMapper
{
    public static SkillDashboardDto ToDto(SkillRecord r) =>
        new(r.Id, r.Name, r.Title, r.Description, r.Doc,
            r.Status, r.Version, r.CreatedAt, r.UpdatedAt);

    public static SkillCommentDto ToDto(SkillCommentRecord c) =>
        new(c.Id, c.SkillId, c.Body, c.CreatedAt, c.UpdatedAt,
            new CommentAuthorDto(
                c.User?.Id ?? c.UserId,
                c.User?.Name,
                c.User?.AvatarUrl));
}

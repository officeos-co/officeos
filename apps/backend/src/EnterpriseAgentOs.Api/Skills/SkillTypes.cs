namespace EnterpriseAgentOs.Api.Skills;

[GraphQLName("Skill")]
public sealed record SkillDashboardDto(
    Guid Id, string Name, string Title, string Description,
    string? Doc, string Status, string Version, bool RequiresApproval,
    DateTime CreatedAt, DateTime UpdatedAt,
    string? Logo, string? License, string? Repository,
    string[] Categories, string[] Keywords,
    string? Readme, string? Changelog,
    [property: GraphQLIgnore] string? AuthorName,
    [property: GraphQLIgnore] string? AuthorUrl,
    [property: GraphQLIgnore] string? ActionsJson,
    [property: GraphQLIgnore] string? ContributorsJson,
    [property: GraphQLIgnore] string? CredentialFieldsJson)
{
    // Batch-populated by the root query to avoid N+1
    [GraphQLIgnore] public int LikesCount { get; init; }
    [GraphQLIgnore] public bool IsLikedByMe { get; init; }
    [GraphQLIgnore] public int CommentCount { get; init; }
    [GraphQLIgnore] public bool IsInstalled { get; init; }
    [GraphQLIgnore] public bool IsConfigured { get; init; }
}

[GraphQLName("SkillTool")]
public sealed record SkillToolDto(string Name, string Description);

[GraphQLName("SkillAuthor")]
public sealed record SkillAuthorDto(string Name, string? Url);

[GraphQLName("SkillContributor")]
public sealed record SkillContributorDto(string Name, string? Url);

[GraphQLName("SkillCredentialField")]
public sealed record SkillCredentialFieldDto(
    string Key, string Label, string Kind, bool Required, string? Placeholder, string? Help,
    SkillOAuth2ConfigDto? Oauth2);

[GraphQLName("SkillOAuth2Config")]
public sealed record SkillOAuth2ConfigDto(string Provider, string[] Scopes);

[GraphQLName("OAuthConnectionStatus")]
public sealed record OAuthConnectionStatusDto(bool Connected, string? Email, string? Scopes, bool NeedsReauth, string[] MissingScopes);

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
    public int GetLikes([Parent] SkillDashboardDto skill) => skill.LikesCount;

    public bool GetLikedByMe([Parent] SkillDashboardDto skill) => skill.IsLikedByMe;

    public int GetCommentsCount([Parent] SkillDashboardDto skill) => skill.CommentCount;

    public bool GetInstalled([Parent] SkillDashboardDto skill) => skill.IsInstalled;

    public bool GetConfigured([Parent] SkillDashboardDto skill) => skill.IsConfigured;

    public IReadOnlyList<SkillCredentialFieldDto> GetCredentialFields([Parent] SkillDashboardDto skill)
    {
        if (string.IsNullOrWhiteSpace(skill.CredentialFieldsJson)) return Array.Empty<SkillCredentialFieldDto>();
        var record = new SkillRecord { CredentialFieldsJson = skill.CredentialFieldsJson };
        return record.GetCredentialFields()
            .Select(f => new SkillCredentialFieldDto(
                f.Key, f.Label, f.Kind, f.Required, f.Placeholder, f.Help,
                f.Oauth2 is not null ? new SkillOAuth2ConfigDto(f.Oauth2.Provider, f.Oauth2.Scopes) : null))
            .ToList();
    }

    public string GetSourceCodeUrl([Parent] SkillDashboardDto skill)
        => $"https://github.com/officeos-co/skill-{skill.Name}";

    public IReadOnlyList<SkillToolDto> GetTools([Parent] SkillDashboardDto skill)
    {
        if (string.IsNullOrWhiteSpace(skill.ActionsJson)) return Array.Empty<SkillToolDto>();
        var record = new SkillRecord { ActionsJson = skill.ActionsJson };
        return record.GetActions()
            .Select(kv => new SkillToolDto(kv.Key, kv.Value.Description))
            .ToList();
    }

    public SkillAuthorDto? GetAuthor([Parent] SkillDashboardDto skill)
    {
        if (skill.AuthorName is null) return null;
        return new SkillAuthorDto(skill.AuthorName, skill.AuthorUrl);
    }

    public IReadOnlyList<SkillContributorDto> GetContributors([Parent] SkillDashboardDto skill)
    {
        if (string.IsNullOrWhiteSpace(skill.ContributorsJson)) return Array.Empty<SkillContributorDto>();
        var record = new SkillRecord { ContributorsJson = skill.ContributorsJson };
        return record.GetContributors()
            .Select(c => new SkillContributorDto(c.Name, c.Url))
            .ToList();
    }
}

internal static class SkillDashboardMapper
{
    public static SkillDashboardDto ToDto(SkillRecord r) =>
        new(r.Id, r.Name, r.Title, r.Description, r.Doc,
            r.Status, r.Version, r.RequiresApproval, r.CreatedAt, r.UpdatedAt,
            r.Logo, r.License, r.Repository,
            r.Categories ?? Array.Empty<string>(), r.Keywords ?? Array.Empty<string>(),
            r.Readme, r.Changelog,
            r.AuthorName, r.AuthorUrl, r.ActionsJson, r.ContributorsJson,
            r.CredentialFieldsJson);

    public static SkillCommentDto ToDto(SkillCommentRecord c) =>
        new(c.Id, c.SkillId, c.Body, c.CreatedAt, c.UpdatedAt,
            new CommentAuthorDto(
                c.User?.Id ?? c.UserId,
                c.User?.Name,
                c.User?.AvatarUrl));
}

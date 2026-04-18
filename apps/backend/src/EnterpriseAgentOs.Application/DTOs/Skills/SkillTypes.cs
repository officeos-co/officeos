namespace EnterpriseAgentOs.Application.DTOs.Skills;

[GraphQLName("Skill")]
public sealed record SkillDashboardDto(
    Guid Id,
    string Name,
    string Title,
    string Description,
    string Emoji,
    string? Doc,
    string? SourceCodeUrl,
    string Status,
    string Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

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

internal static class SkillDashboardMapper
{
    public static SkillDashboardDto ToDto(EnterpriseAgentOs.Domain.Models.SkillRecord r) =>
        new(r.Id, r.Name, r.Title, r.Description, r.Emoji, r.Doc,
            r.SourceCodeUrl, r.Status, r.Version, r.CreatedAt, r.UpdatedAt);

    public static SkillCommentDto ToDto(EnterpriseAgentOs.Domain.Models.SkillCommentRecord c) =>
        new(c.Id, c.SkillId, c.Body, c.CreatedAt, c.UpdatedAt,
            new CommentAuthorDto(
                c.User?.Id ?? c.UserId,
                c.User?.Name,
                c.User?.AvatarUrl));
}

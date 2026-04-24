namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class SkillEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Doc { get; set; }
    public string Source { get; set; } = "builtin";
    public string? Logo { get; set; }
    public string? License { get; set; }
    public string? Repository { get; set; }
    public bool RequiresApproval { get; set; }
    public string? Readme { get; set; }
    public string? Changelog { get; set; }
    public string? Category { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorUrl { get; set; }
    public string[]? Categories { get; set; }
    public string[]? Keywords { get; set; }
    public string? ActionsJson { get; set; }
    public string? CredentialFieldsJson { get; set; }
    public string? ContributorsJson { get; set; }
    public string? BundleS3Key { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "active";
    public string? BuildError { get; set; }
    public string? GitHubRepoUrl { get; set; }
    public string? GitHubBranch { get; set; }
    public bool IsSystem { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserEntity? Owner { get; set; }
}

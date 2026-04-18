namespace EnterpriseAgentOs.Domain.Models;

public sealed class SkillRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Doc { get; set; }

    /// <summary>"builtin" | "upload" | "github"</summary>
    [Required, MaxLength(16)]
    public string Source { get; set; } = "builtin";

    public string? Logo { get; set; }

    [MaxLength(64)]
    public string? License { get; set; }

    [MaxLength(512)]
    public string? Repository { get; set; }

    public bool RequiresApproval { get; set; }

    public string? Readme { get; set; }
    public string? Changelog { get; set; }

    [MaxLength(64)]
    public string? Category { get; set; }

    [MaxLength(128)]
    public string? AuthorName { get; set; }

    [MaxLength(512)]
    public string? AuthorUrl { get; set; }

    public string[]? Categories { get; set; }
    public string[]? Keywords { get; set; }

    /// <summary>JSON object: { "actionName": { description, params, returns } }</summary>
    public string? ActionsJson { get; set; }

    /// <summary>JSON array of credential field definitions.</summary>
    public string? CredentialFieldsJson { get; set; }

    /// <summary>JSON array of { name, url } contributor objects.</summary>
    public string? ContributorsJson { get; set; }

    /// <summary>S3 key for the built JS bundle, e.g. "skills/github/github.js". Null for builtin skills baked into the Docker image.</summary>
    [MaxLength(512)]
    public string? BundleS3Key { get; set; }

    [MaxLength(32)]
    public string Version { get; set; } = "1.0.0";

    /// <summary>"active" | "disabled" | "building" | "failed"</summary>
    [Required, MaxLength(16)]
    public string Status { get; set; } = "active";

    public string? BuildError { get; set; }

    [MaxLength(512)]
    public string? GitHubRepoUrl { get; set; }

    [MaxLength(128)]
    public string? GitHubBranch { get; set; }

    public bool IsSystem { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserRecord? Owner { get; set; }
}

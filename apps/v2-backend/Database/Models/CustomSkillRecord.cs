namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class CustomSkillRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(16)]
    public string Source { get; set; } = "upload";
    [MaxLength(512)]
    public string? GitHubRepoUrl { get; set; }
    [MaxLength(128)]
    public string? GitHubBranch { get; set; }
    [MaxLength(512)]
    public string? BundlePath { get; set; }
    [Required, MaxLength(32)]
    public string BuildStatus { get; set; } = "pending";
    public string? BuildError { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastSyncAt { get; set; }

    public UserRecord? Owner { get; set; }
}

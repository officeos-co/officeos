namespace OffceOs.Domain.Features.Management;

public sealed class UserRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [Required, MaxLength(256)]
    public string Email { get; init; } = string.Empty;
    [MaxLength(256)]
    public string? Name { get; set; }
    [MaxLength(1024)]
    public string? AvatarUrl { get; set; }
    [MaxLength(256)]
    public string? GoogleSubjectId { get; init; }
    [MaxLength(256)]
    public string? GitHubSubjectId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    // Profile fields — editable via AuthMutations.updateProfile
    [MaxLength(128)]
    public string? DisplayName { get; set; }
    [MaxLength(64)]
    public string? Timezone { get; set; }
    /// <summary>JSON blob of notification preferences, e.g. {"taskCompletions":true,"email":false,"channelMessages":true}.</summary>
    public string? NotificationPrefsJson { get; set; }

    /// <summary>Free-text personal preferences that apply to all agents (e.g. "keep explanations brief").</summary>
    [MaxLength(4000)]
    public string? Preferences { get; set; }

    public UserSubscription? Subscription { get; init; }
}

namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string GoogleSubjectId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public string? DisplayName { get; set; }
    public string? Timezone { get; set; }
    public string? NotificationPrefsJson { get; set; }
    public string? Preferences { get; set; }
}

namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// Org-level channel connection. One per external platform (e.g. one Slack
/// workspace, one Telegram bot). Config is a DataProtection-wrapped JSON blob
/// whose schema varies by channel type.
/// </summary>
public sealed class ChannelConnectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string ChannelType { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Protected JSON object with platform tokens/secrets.</summary>
    public string? EncryptedConfig { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>FK to UserRecord — the admin who created this connection.</summary>
    public Guid? CreatedById { get; set; }
    public UserRecord? CreatedBy { get; set; }
}

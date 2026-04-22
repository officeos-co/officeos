namespace EnterpriseAgentOs.Domain.Channels;

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

    /// <summary>Whether a test/welcome message has been sent on this connection.</summary>
    public bool TestMessageSent { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>FK to UserRecord — the admin who created this connection.</summary>
    public Guid? CreatedById { get; set; }
    public UserRecord? CreatedBy { get; set; }

    // ── Domain logic ─────────────────────────────────────────────────

    public bool IsWhatsApp => string.Equals(ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether this connection has platform credentials configured.</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(EncryptedConfig);

    /// <summary>Whether this connection still needs a test message.</summary>
    public bool NeedsTestMessage => !TestMessageSent;

    public void MarkTestMessageSent() => TestMessageSent = true;

    /// <summary>Factory: validates channel type and creates a new connection.</summary>
    public static ChannelConnectionRecord Create(string channelType, string displayName, Guid createdById)
    {
        var definition = ChannelTypes.GetByType(channelType)
            ?? throw new InvalidOperationException($"Unknown channel type: {channelType}");

        return new ChannelConnectionRecord
        {
            ChannelType = definition.Type, // normalized
            DisplayName = displayName,
            CreatedById = createdById,
        };
    }

    /// <summary>Apply a partial update. Only non-null fields are changed.</summary>
    public void ApplyUpdate(string? displayName, bool? enabled)
    {
        if (displayName is not null) DisplayName = displayName;
        if (enabled.HasValue) Enabled = enabled.Value;
    }

    /// <summary>
    /// Extract the owner identifier from channel credentials JSON.
    /// For WhatsApp, this is the raw me.id field — platform-specific
    /// normalization (e.g. stripping device suffixes) happens in the gateway.
    /// </summary>
    public static string? ExtractOwnerIdentifier(string credsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(credsJson);
            // WhatsApp: me.id
            if (doc.RootElement.TryGetProperty("me", out var me) &&
                me.TryGetProperty("id", out var idProp))
                return idProp.GetString();
        }
        catch { /* malformed creds */ }
        return null;
    }
}

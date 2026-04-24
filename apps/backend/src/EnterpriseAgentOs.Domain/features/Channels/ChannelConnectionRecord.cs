namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Lightweight metadata record for a channel connection. The backend stores only
/// identity + display info. All platform credentials, config, and connection state
/// live in the channel microservice (apps/channels).
/// </summary>
public sealed class ChannelConnectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string ChannelType { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>FK to UserRecord — the admin who created this connection.</summary>
    public Guid? CreatedById { get; init; }
    public UserRecord? CreatedBy { get; init; }

    // ── Domain logic ─────────────────────────────────────────────────

    /// <summary>Factory: validates channel type and creates a new connection.</summary>
    public static ChannelConnectionRecord Create(string channelType, string displayName, Guid createdById)
    {
        var definition = ChannelTypes.GetByType(channelType)
            ?? throw new InvalidOperationException($"Unknown channel type: {channelType}");

        return new ChannelConnectionRecord
        {
            ChannelType = definition.Type,
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
}

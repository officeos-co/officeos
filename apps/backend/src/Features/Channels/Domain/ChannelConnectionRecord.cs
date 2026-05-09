namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Lightweight metadata record for a channel connection. The backend stores only
/// identity + display info. All platform credentials, config, and connection state
/// live in the channel microservice (packages/channels).
/// </summary>
public sealed class ChannelConnectionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ChannelType ChannelType { get; init; }

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>FK to UserRecord — the admin who created this connection.</summary>
    public Guid? CreatedById { get; init; }

    /// <summary>Encrypted channel credentials JSON. Decrypted only when passed to the sidecar.</summary>
    public string? EncryptedCreds { get; set; }

    public UserRecord? CreatedBy { get; init; }

    public IReadOnlyList<AgentChannelBindingRecord> Bindings { get; init; } = [];

    // ── Domain logic ─────────────────────────────────────────────────

    /// <summary>Factory: validates channel type and creates a new connection.</summary>
    public static ChannelConnectionRecord Create(ChannelType channelType, string displayName, Guid createdById)
    {
        // Validate that the type has a known definition
        _ = ChannelTypes.GetByType(channelType.ToStorageString())
            ?? throw new InvalidOperationException($"Unknown channel type: {channelType}");

        return new ChannelConnectionRecord
        {
            ChannelType = channelType,
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

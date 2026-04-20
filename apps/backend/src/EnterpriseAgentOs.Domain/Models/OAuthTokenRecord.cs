namespace EnterpriseAgentOs.Domain.Models;

/// <summary>
/// Stores OAuth2 tokens for skill credentials. One row per provider per user.
/// Multiple skills can share the same token row (e.g., all Google skills share one Google OAuth connection).
/// The backend auto-refreshes expired access tokens using the stored refresh token.
/// </summary>
public sealed class OAuthTokenRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>OAuth2 provider identifier (e.g. "google", "microsoft", "github").</summary>
    [Required, MaxLength(32)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Encrypted access token.</summary>
    public string? EncryptedAccessToken { get; set; }

    /// <summary>Encrypted refresh token.</summary>
    public string? EncryptedRefreshToken { get; set; }

    /// <summary>Combined scopes granted by the user (union of all skills requesting this provider).</summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>When the access token expires (UTC).</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>User email from the OAuth provider (for display in dashboard).</summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

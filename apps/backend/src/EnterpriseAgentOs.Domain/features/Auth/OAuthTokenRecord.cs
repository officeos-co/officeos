namespace EnterpriseAgentOs.Domain.Features.Auth;

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

    /// <summary>Individual scopes granted by the user for this provider.</summary>
    public ICollection<OAuthGrantedScopeRecord> GrantedScopes { get; init; } = new List<OAuthGrantedScopeRecord>();

    /// <summary>When the access token expires (UTC).</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>User email from the OAuth provider (for display in dashboard).</summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Domain behavior ──

    /// <summary>Returns true if every scope in <paramref name="requiredScopes"/> is granted.</summary>
    public bool CoversScopes(IEnumerable<string> requiredScopes)
    {
        var granted = GrantedScopes.Select(s => s.Scope).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return requiredScopes.All(s => granted.Contains(s));
    }

    /// <summary>Returns the subset of <paramref name="requiredScopes"/> not yet granted.</summary>
    public IReadOnlyList<string> MissingScopes(IEnumerable<string> requiredScopes)
    {
        var granted = GrantedScopes.Select(s => s.Scope).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return requiredScopes.Where(s => !granted.Contains(s)).ToList();
    }

    /// <summary>Returns all granted scopes as a set for bulk comparisons.</summary>
    public HashSet<string> GetScopeSet() =>
        GrantedScopes.Select(s => s.Scope).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Replaces all granted scopes with <paramref name="scopes"/>.
    /// Call this after a token exchange — Google returns the full set of granted scopes.
    /// </summary>
    public void ReplaceScopes(IEnumerable<string> scopes)
    {
        GrantedScopes.Clear();
        foreach (var scope in scopes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            GrantedScopes.Add(new OAuthGrantedScopeRecord
            {
                OAuthTokenId = Id,
                Scope = scope,
            });
        }
    }

    /// <summary>
    /// Returns the union of existing granted scopes and <paramref name="additionalScopes"/>.
    /// Used by the Start endpoint to build the full scope list for the authorization URL.
    /// </summary>
    public HashSet<string> MergedScopesWith(IEnumerable<string> additionalScopes)
    {
        var merged = GetScopeSet();
        foreach (var s in additionalScopes)
            merged.Add(s);
        return merged;
    }
}

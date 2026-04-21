namespace EnterpriseAgentOs.Domain.Auth;

/// <summary>
/// One granted OAuth2 scope per provider token. Child of OAuthTokenRecord.
/// Replaces the old space-delimited Scopes string for type-safe scope queries.
/// </summary>
public sealed class OAuthGrantedScopeRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OAuthTokenId { get; init; }
    public OAuthTokenRecord OAuthToken { get; init; } = null!;

    /// <summary>Full scope URI, e.g. "https://www.googleapis.com/auth/gmail.modify".</summary>
    [Required, MaxLength(512)]
    public string Scope { get; set; } = string.Empty;
}

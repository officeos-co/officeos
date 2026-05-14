namespace OffceOs.Database.Models;

public sealed class OAuthTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? EncryptedAccessToken { get; set; }
    public string? EncryptedRefreshToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OAuthGrantedScopeEntity> GrantedScopes { get; set; } = new();
}

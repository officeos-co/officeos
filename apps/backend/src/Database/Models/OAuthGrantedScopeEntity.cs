namespace OffceOs.Database.Models;

public sealed class OAuthGrantedScopeEntity
{
    public Guid Id { get; set; }
    public Guid OAuthTokenId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public OAuthTokenEntity? OAuthToken { get; set; }
}

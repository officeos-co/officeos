using EnterpriseAgentOs.Infrastructure.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Management;

internal sealed class OAuthTokenRepository : IOAuthTokenRepository
{
    private readonly EaosDbContext _db;

    public OAuthTokenRepository(EaosDbContext db) => _db = db;

    public async Task<OAuthTokenRecord?> GetByProviderAsync(string provider, CancellationToken ct = default)
    {
        var entity = await _db.OAuthTokens.AsNoTracking()
            .Include(t => t.GrantedScopes)
            .FirstOrDefaultAsync(t => t.Provider == provider, ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default)
    {
        var existing = await _db.OAuthTokens
            .Include(t => t.GrantedScopes)
            .FirstOrDefaultAsync(t => t.Provider == token.Provider, ct);

        if (existing is null)
        {
            existing = new OAuthTokenEntity
            {
                Id = token.Id,
                Provider = token.Provider,
                CreatedAt = token.CreatedAt,
            };
            _db.OAuthTokens.Add(existing);
        }

        existing.EncryptedAccessToken = token.EncryptedAccessToken;
        existing.EncryptedRefreshToken = token.EncryptedRefreshToken ?? existing.EncryptedRefreshToken;
        existing.ExpiresAtUtc = token.ExpiresAtUtc;
        existing.Email = token.Email;
        existing.UpdatedAt = DateTime.UtcNow;

        _db.OAuthGrantedScopes.RemoveRange(existing.GrantedScopes);
        existing.GrantedScopes = token.GrantedScopes
            .Select(s => new OAuthGrantedScopeEntity
            {
                Id = Guid.NewGuid(),
                OAuthTokenId = existing.Id,
                Scope = s.Scope,
            })
            .ToList();

        await _db.SaveChangesAsync(ct);
    }

    private static OAuthTokenRecord ToRecord(OAuthTokenEntity entity)
    {
        var record = new OAuthTokenRecord
        {
            Id = entity.Id,
            Provider = entity.Provider,
            EncryptedAccessToken = entity.EncryptedAccessToken,
            EncryptedRefreshToken = entity.EncryptedRefreshToken,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

        record.ReplaceScopes(entity.GrantedScopes.Select(s => s.Scope));
        return record;
    }
}

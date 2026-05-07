using EnterpriseAgentOs.Infrastructure.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Management;

internal sealed class OAuthTokenRepository : IOAuthTokenRepository
{
    private readonly EaosDbContext _db;

    public OAuthTokenRepository(EaosDbContext db) => _db = db;

    public async Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default)
    {
        var query = _db.OAuthTokens.AsNoTracking().Include(t => t.GrantedScopes).AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(t => t.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.Provider))
            query = query.Where(t => t.Provider == filter.Provider);

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(t => t.Email == filter.Email);

        var entity = await query.FirstOrDefaultAsync(ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default)
    {
        var existing = await _db.OAuthTokens
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

        if (_db.Entry(existing).State == EntityState.Added)
        {
            existing.GrantedScopes = token.GrantedScopes
                .Select(s => CreateScope(existing.Id, s.Scope))
                .ToList();
        }
        else
        {
            await _db.OAuthGrantedScopes
                .Where(s => s.OAuthTokenId == existing.Id)
                .ExecuteDeleteAsync(ct);

            await _db.OAuthGrantedScopes.AddRangeAsync(
                token.GrantedScopes.Select(s => CreateScope(existing.Id, s.Scope)),
                ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static OAuthGrantedScopeEntity CreateScope(Guid tokenId, string scope) => new()
    {
        Id = Guid.NewGuid(),
        OAuthTokenId = tokenId,
        Scope = scope,
    };

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

using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Integrations;

internal sealed class IntegrationCredentialRepository : IIntegrationCredentialRepository
{
    private readonly EaosDbContext _db;

    public IntegrationCredentialRepository(EaosDbContext db) => _db = db;

    public async Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
    {
        var query = _db.IntegrationCredentials.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(c => c.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue)
            query = query.Where(c => c.OwnerId == filter.OwnerId.Value);

        if (!string.IsNullOrEmpty(filter.IntegrationName))
            query = query.Where(c => c.IntegrationName == filter.IntegrationName);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : new IntegrationCredentialRecord
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId,
            IntegrationName = entity.IntegrationName,
            EncryptedCredentials = entity.EncryptedCredentials,
            ConfiguredAt = entity.ConfiguredAt,
        };
    }

    public async Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct)
    {
        var existing = await _db.IntegrationCredentials
            .FirstOrDefaultAsync(c => c.OwnerId == credential.OwnerId && c.IntegrationName == credential.IntegrationName, ct);
        if (existing is not null)
        {
            existing.EncryptedCredentials = credential.EncryptedCredentials;
            existing.ConfiguredAt = credential.ConfiguredAt;
        }
        else
        {
            _db.IntegrationCredentials.Add(new IntegrationCredentialEntity
            {
                Id = credential.Id,
                OwnerId = credential.OwnerId,
                IntegrationName = credential.IntegrationName,
                EncryptedCredentials = credential.EncryptedCredentials,
                ConfiguredAt = credential.ConfiguredAt,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid ownerId, string integrationName, CancellationToken ct)
    {
        await _db.IntegrationCredentials
            .Where(c => c.OwnerId == ownerId && c.IntegrationName == integrationName)
            .ExecuteDeleteAsync(ct);
    }
}

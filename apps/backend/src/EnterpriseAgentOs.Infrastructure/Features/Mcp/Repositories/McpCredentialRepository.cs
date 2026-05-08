using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Agents.Integrations;

internal sealed class IntegrationCredentialRepository : IIntegrationCredentialRepository
{
    private readonly EaosDbContext _db;

    public IntegrationCredentialRepository(EaosDbContext db) => _db = db;

    public async Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
    {
        var query = _db.McpCredentials.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(c => c.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.IntegrationName))
            query = query.Where(c => c.IntegrationName == filter.IntegrationName);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : new IntegrationCredentialRecord
        {
            Id = entity.Id,
            IntegrationName = entity.IntegrationName,
            EncryptedCredentials = entity.EncryptedCredentials,
            ConfiguredAt = entity.ConfiguredAt,
        };
    }

    public async Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct)
    {
        var existing = await _db.McpCredentials
            .FirstOrDefaultAsync(c => c.IntegrationName == credential.IntegrationName, ct);
        if (existing is not null)
        {
            existing.EncryptedCredentials = credential.EncryptedCredentials;
            existing.ConfiguredAt = credential.ConfiguredAt;
        }
        else
        {
            _db.McpCredentials.Add(new IntegrationCredentialEntity
            {
                Id = credential.Id,
                IntegrationName = credential.IntegrationName,
                EncryptedCredentials = credential.EncryptedCredentials,
                ConfiguredAt = credential.ConfiguredAt,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string integrationName, CancellationToken ct)
    {
        await _db.McpCredentials.Where(c => c.IntegrationName == integrationName).ExecuteDeleteAsync(ct);
    }
}

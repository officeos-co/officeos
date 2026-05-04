using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class McpCredentialRepository : IMcpCredentialRepository
{
    private readonly EaosDbContext _db;

    public McpCredentialRepository(EaosDbContext db) => _db = db;

    public async Task<McpCredentialRecord?> GetByAsync(McpCredentialFilter filter, CancellationToken ct = default)
    {
        var query = _db.McpCredentials.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(c => c.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.ServerName))
            query = query.Where(c => c.McpServerName == filter.ServerName);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : new McpCredentialRecord
        {
            Id = entity.Id,
            McpServerName = entity.McpServerName,
            EncryptedCredentials = entity.EncryptedCredentials,
            ConfiguredAt = entity.ConfiguredAt,
        };
    }

    public async Task UpsertAsync(McpCredentialRecord credential, CancellationToken ct)
    {
        var existing = await _db.McpCredentials
            .FirstOrDefaultAsync(c => c.McpServerName == credential.McpServerName, ct);
        if (existing is not null)
        {
            existing.EncryptedCredentials = credential.EncryptedCredentials;
            existing.ConfiguredAt = credential.ConfiguredAt;
        }
        else
        {
            _db.McpCredentials.Add(new McpCredentialEntity
            {
                Id = credential.Id,
                McpServerName = credential.McpServerName,
                EncryptedCredentials = credential.EncryptedCredentials,
                ConfiguredAt = credential.ConfiguredAt,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string serverName, CancellationToken ct)
    {
        await _db.McpCredentials.Where(c => c.McpServerName == serverName).ExecuteDeleteAsync(ct);
    }
}

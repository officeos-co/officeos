using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.AgentRoutines;
namespace OffceOs.Infrastructure.Features.AgentRoutines;

internal sealed class AgentRoutineCredentialRepository : IAgentRoutineCredentialRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public AgentRoutineCredentialRepository(EaosDbContext eaosDbContext)
    {
        _eaosDbContext = eaosDbContext;
    }

    public async Task<IReadOnlyList<AgentRoutineCredentialRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.AgentRoutineCredentials
            .AsNoTracking()
            .Where(credential => credential.WorkspaceId == workspaceId)
            .OrderBy(credential => credential.Name)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<AgentRoutineCredentialRecord?> GetByNameAsync(Guid workspaceId, string name, CancellationToken ct = default)
    {
        var normalized = NormalizeName(name);
        var entity = await _eaosDbContext.AgentRoutineCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(credential => credential.WorkspaceId == workspaceId && credential.Name == normalized, ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<AgentRoutineCredentialRecord> UpsertAsync(AgentRoutineCredentialRecord record, CancellationToken ct = default)
    {
        var normalized = NormalizeName(record.Name);
        var entity = await _eaosDbContext.AgentRoutineCredentials
            .FirstOrDefaultAsync(credential => credential.WorkspaceId == record.WorkspaceId && credential.Name == normalized, ct);

        if (entity is null)
        {
            entity = ToEntity(record, normalized);
            _eaosDbContext.AgentRoutineCredentials.Add(entity);
        }
        else
        {
            entity.OwnerId = record.OwnerId;
            entity.Provider = record.Provider;
            entity.AuthKind = record.AuthKind;
            entity.EncryptedSecret = record.EncryptedSecret;
            entity.PublicMetadataJson = record.PublicMetadataJson;
            entity.ScopesJson = record.ScopesJson;
            entity.ExpiresAtUtc = record.ExpiresAtUtc;
            entity.UpdatedAt = record.UpdatedAt;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid workspaceId, string name, CancellationToken ct = default)
    {
        var normalized = NormalizeName(name);
        var deleted = await _eaosDbContext.AgentRoutineCredentials
            .Where(credential => credential.WorkspaceId == workspaceId && credential.Name == normalized)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    public async Task MarkUsedAsync(Guid id, DateTime usedAt, CancellationToken ct = default)
    {
        await _eaosDbContext.AgentRoutineCredentials
            .Where(credential => credential.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(credential => credential.LastUsedAt, usedAt)
                .SetProperty(credential => credential.UpdatedAt, usedAt), ct);
    }

    private static AgentRoutineCredentialEntity ToEntity(AgentRoutineCredentialRecord record, string normalizedName) => new()
    {
        Id = record.Id,
        OwnerId = record.OwnerId,
        WorkspaceId = record.WorkspaceId,
        Name = normalizedName,
        Provider = record.Provider,
        AuthKind = record.AuthKind,
        EncryptedSecret = record.EncryptedSecret,
        PublicMetadataJson = record.PublicMetadataJson,
        ScopesJson = record.ScopesJson,
        ExpiresAtUtc = record.ExpiresAtUtc,
        LastUsedAt = record.LastUsedAt,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
    };

    private static AgentRoutineCredentialRecord ToRecord(AgentRoutineCredentialEntity entity) => new()
    {
        Id = entity.Id,
        OwnerId = entity.OwnerId,
        WorkspaceId = entity.WorkspaceId,
        Name = entity.Name,
        Provider = entity.Provider,
        AuthKind = entity.AuthKind,
        EncryptedSecret = entity.EncryptedSecret,
        PublicMetadataJson = entity.PublicMetadataJson,
        ScopesJson = entity.ScopesJson,
        ExpiresAtUtc = entity.ExpiresAtUtc,
        LastUsedAt = entity.LastUsedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Credential name is required.");

        return name.Trim().ToLowerInvariant();
    }
}

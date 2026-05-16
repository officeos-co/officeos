using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Integrations;
namespace OffceOs.Infrastructure.Features.Integrations;

internal sealed class IntegrationCredentialRepository : IIntegrationCredentialRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationCredentialRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<IntegrationCredentialRecord>> ListAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
        => await BuildQuery(filter).Select(entity => ToRecord(entity)).ToListAsync(ct);

    public async Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
    {
        var entity = await BuildQuery(filter).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct)
    {
        var existing = await _eaosDbContext.IntegrationCredentials
            .FirstOrDefaultAsync(c => c.WorkspaceId == credential.WorkspaceId
                && c.IntegrationName == credential.IntegrationName, ct);
        if (existing is not null)
        {
            existing.OwnerId = credential.OwnerId;
            existing.AuthKind = credential.AuthKind;
            existing.State = credential.State.ToStorageString();
            existing.EncryptedSecretEnvelope = credential.EncryptedSecretEnvelope;
            existing.PublicAuthMetadataJson = credential.PublicAuthMetadataJson;
            existing.ScopesJson = credential.ScopesJson;
            existing.ExpiresAtUtc = credential.ExpiresAtUtc;
            existing.ValidatedAt = credential.ValidatedAt;
            existing.ArchivedAt = credential.ArchivedAt;
            existing.CreatedBy = credential.CreatedBy;
            existing.ConfiguredAt = credential.ConfiguredAt;
            existing.UpdatedAt = credential.UpdatedAt;
        }
        else
        {
            _eaosDbContext.IntegrationCredentials.Add(new IntegrationCredentialEntity
            {
                Id = credential.Id,
                OwnerId = credential.OwnerId,
                WorkspaceId = credential.WorkspaceId,
                IntegrationName = credential.IntegrationName,
                AuthKind = credential.AuthKind,
                State = credential.State.ToStorageString(),
                EncryptedSecretEnvelope = credential.EncryptedSecretEnvelope,
                PublicAuthMetadataJson = credential.PublicAuthMetadataJson,
                ScopesJson = credential.ScopesJson,
                ExpiresAtUtc = credential.ExpiresAtUtc,
                ValidatedAt = credential.ValidatedAt,
                LastUsedAt = credential.LastUsedAt,
                ArchivedAt = credential.ArchivedAt,
                CreatedBy = credential.CreatedBy,
                ConfiguredAt = credential.ConfiguredAt,
                UpdatedAt = credential.UpdatedAt,
            });
        }
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid workspaceId, string integrationName, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _eaosDbContext.IntegrationCredentials
            .Where(c => c.WorkspaceId == workspaceId && c.IntegrationName == integrationName)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.State, IntegrationCredentialState.Archived.ToStorageString())
                .SetProperty(c => c.ArchivedAt, now)
                .SetProperty(c => c.UpdatedAt, now), ct);
    }

    public async Task DeleteAsync(Guid ownerId, string integrationName, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.IntegrationCredentials
            .Where(c => c.IntegrationName == integrationName);

        if (workspaceId.HasValue)
            query = query.Where(c => c.WorkspaceId == workspaceId.Value);
        else
            query = query.Where(c => c.OwnerId == ownerId);

        await query.ExecuteDeleteAsync(ct);
    }

    public async Task MarkUsedAsync(Guid id, DateTime usedAt, CancellationToken ct = default)
    {
        await _eaosDbContext.IntegrationCredentials
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.LastUsedAt, usedAt)
                .SetProperty(c => c.UpdatedAt, usedAt), ct);
    }

    private IQueryable<IntegrationCredentialEntity> BuildQuery(IntegrationCredentialFilter filter)
    {
        var query = _eaosDbContext.IntegrationCredentials.AsNoTracking().AsQueryable();

        if (!filter.IncludeArchived)
            query = query.Where(c => c.State != IntegrationCredentialState.Archived.ToStorageString());

        if (filter.Id.HasValue)
            query = query.Where(c => c.Id == filter.Id.Value);

        if (filter.OwnerId.HasValue && !filter.WorkspaceId.HasValue)
            query = query.Where(c => c.OwnerId == filter.OwnerId.Value);

        if (filter.WorkspaceId.HasValue)
            query = query.Where(c => c.WorkspaceId == filter.WorkspaceId.Value);

        if (!string.IsNullOrEmpty(filter.IntegrationName))
            query = query.Where(c => c.IntegrationName == filter.IntegrationName);

        return query;
    }

    private static IntegrationCredentialRecord ToRecord(IntegrationCredentialEntity entity) => new()
    {
        Id = entity.Id,
        OwnerId = entity.OwnerId,
        WorkspaceId = entity.WorkspaceId,
        IntegrationName = entity.IntegrationName,
        AuthKind = entity.AuthKind,
        State = entity.State.ToIntegrationCredentialState(),
        EncryptedSecretEnvelope = entity.EncryptedSecretEnvelope,
        PublicAuthMetadataJson = entity.PublicAuthMetadataJson,
        ScopesJson = entity.ScopesJson,
        ExpiresAtUtc = entity.ExpiresAtUtc,
        ValidatedAt = entity.ValidatedAt,
        LastUsedAt = entity.LastUsedAt,
        ArchivedAt = entity.ArchivedAt,
        CreatedBy = entity.CreatedBy,
        ConfiguredAt = entity.ConfiguredAt,
        UpdatedAt = entity.UpdatedAt,
    };
}

internal static class IntegrationCredentialStateTranslator
{
    public static string ToStorageString(this IntegrationCredentialState state) => state switch
    {
        IntegrationCredentialState.Active => "active",
        IntegrationCredentialState.ValidationFailed => "validation_failed",
        IntegrationCredentialState.Archived => "archived",
        _ => "active",
    };

    public static IntegrationCredentialState ToIntegrationCredentialState(this string? state) => state switch
    {
        "validation_failed" => IntegrationCredentialState.ValidationFailed,
        "archived" => IntegrationCredentialState.Archived,
        _ => IntegrationCredentialState.Active,
    };
}

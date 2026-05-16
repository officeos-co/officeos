using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Providers.Domain;
namespace OffceOs.Features.Providers.Infrastructure;

internal sealed class ProviderResourceRepository : IProviderResourceRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public ProviderResourceRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<ProviderResourceRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.ProviderResources
            .AsNoTracking()
            .Where(provider => provider.WorkspaceId == workspaceId)
            .OrderBy(provider => provider.Name)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<ProviderResourceRecord?> GetByNameAsync(Guid workspaceId, string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.ProviderResources
            .AsNoTracking()
            .FirstOrDefaultAsync(provider => provider.WorkspaceId == workspaceId && provider.Name == normalized, ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<ProviderResourceRecord> UpsertAsync(ProviderResourceRecord record, CancellationToken ct = default)
    {
        var normalized = record.Name.Trim().ToLowerInvariant();
        var entity = await _eaosDbContext.ProviderResources
            .FirstOrDefaultAsync(provider => provider.WorkspaceId == record.WorkspaceId && provider.Name == normalized, ct);

        if (entity is null)
        {
            entity = ToEntity(record with { Name = normalized });
            _eaosDbContext.ProviderResources.Add(entity);
        }
        else
        {
            entity.Type = record.Type.Trim().ToLowerInvariant();
            entity.DisplayName = record.DisplayName;
            entity.Enabled = record.Enabled;
            entity.DefaultModel = record.DefaultModel;
            entity.AllowedModelsJson = JsonSerializer.Serialize(record.Models);
            entity.AuthKind = record.AuthKind;
            entity.EncryptedCredentialsJson = record.EncryptedCredentialsJson;
            entity.Phase = record.Phase;
            entity.StatusMessage = record.StatusMessage;
            entity.Account = record.Account;
            entity.ExpiresAt = record.ExpiresAt;
            entity.LastValidatedAt = record.LastValidatedAt;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid workspaceId, string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return await _eaosDbContext.ProviderResources
            .Where(provider => provider.WorkspaceId == workspaceId && provider.Name == normalized)
            .ExecuteDeleteAsync(ct) > 0;
    }

    private static ProviderResourceRecord ToRecord(ProviderResourceEntity entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        Name = entity.Name,
        Type = entity.Type,
        DisplayName = entity.DisplayName,
        Enabled = entity.Enabled,
        DefaultModel = entity.DefaultModel,
        Models = ParseModels(entity.AllowedModelsJson),
        AuthKind = entity.AuthKind,
        EncryptedCredentialsJson = entity.EncryptedCredentialsJson,
        Phase = entity.Phase,
        StatusMessage = entity.StatusMessage,
        Account = entity.Account,
        ExpiresAt = entity.ExpiresAt,
        LastValidatedAt = entity.LastValidatedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    private static ProviderResourceEntity ToEntity(ProviderResourceRecord withName) => new()
    {
        Id = withName.Id,
        WorkspaceId = withName.WorkspaceId,
        Name = withName.Name,
        Type = withName.Type.Trim().ToLowerInvariant(),
        DisplayName = withName.DisplayName,
        Enabled = withName.Enabled,
        DefaultModel = withName.DefaultModel,
        AllowedModelsJson = JsonSerializer.Serialize(withName.Models),
        AuthKind = withName.AuthKind,
        EncryptedCredentialsJson = withName.EncryptedCredentialsJson,
        Phase = withName.Phase,
        StatusMessage = withName.StatusMessage,
        Account = withName.Account,
        ExpiresAt = withName.ExpiresAt,
        LastValidatedAt = withName.LastValidatedAt,
        CreatedAt = withName.CreatedAt,
        UpdatedAt = withName.UpdatedAt,
    };

    private static IReadOnlyList<string> ParseModels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

namespace OffceOs.Infrastructure.Features.Management;

internal sealed class OrganizationProviderProfileRepository : IOrganizationProviderProfileRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationProviderProfileRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<OrganizationProviderProfileRecord>> ListAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default)
    {
        var entities = await BuildQuery(filter)
            .OrderBy(p => p.Provider)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<OrganizationProviderProfileRecord?> GetByAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default)
    {
        var entity = await BuildQuery(filter).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<OrganizationProviderProfileRecord> UpsertAsync(OrganizationProviderProfileRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrganizationProviderProfiles
            .FirstOrDefaultAsync(p => p.OrganizationId == record.OrganizationId && p.Provider == record.Provider, ct);

        if (entity is null)
        {
            entity = ToEntity(record);
            _eaosDbContext.OrganizationProviderProfiles.Add(entity);
        }
        else
        {
            entity.DisplayName = record.DisplayName;
            entity.AllowedModelsJson = NormalizeJsonArray(record.AllowedModelsJson);
            entity.EncryptedApiKey = record.EncryptedApiKey;
            entity.Enabled = record.Enabled;
            entity.ConfiguredAt = record.ConfiguredAt;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(OrganizationProviderProfileFilter filter, CancellationToken ct = default)
    {
        var deleted = await BuildQuery(filter).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private IQueryable<OrganizationProviderProfileEntity> BuildQuery(OrganizationProviderProfileFilter filter)
    {
        var query = _eaosDbContext.OrganizationProviderProfiles.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(p => p.Id == filter.Id.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(p => p.OrganizationId == filter.OrganizationId.Value);

        if (filter.WorkspaceId.HasValue)
        {
            query = query.Where(p => _eaosDbContext.Workspaces
                .Any(w => w.Id == filter.WorkspaceId.Value && w.OrganizationId == p.OrganizationId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(p => p.Provider == filter.Provider);

        if (filter.Enabled.HasValue)
            query = query.Where(p => p.Enabled == filter.Enabled.Value);

        return query;
    }

    private static OrganizationProviderProfileRecord ToRecord(OrganizationProviderProfileEntity entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        Provider = entity.Provider,
        DisplayName = entity.DisplayName,
        AllowedModelsJson = entity.AllowedModelsJson,
        EncryptedApiKey = entity.EncryptedApiKey,
        Enabled = entity.Enabled,
        ConfiguredAt = entity.ConfiguredAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    private static OrganizationProviderProfileEntity ToEntity(OrganizationProviderProfileRecord record) => new()
    {
        Id = record.Id,
        OrganizationId = record.OrganizationId,
        Provider = record.Provider,
        DisplayName = record.DisplayName,
        AllowedModelsJson = NormalizeJsonArray(record.AllowedModelsJson),
        EncryptedApiKey = record.EncryptedApiKey,
        Enabled = record.Enabled,
        ConfiguredAt = record.ConfiguredAt,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
    };

    private static string NormalizeJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "[]";

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array ? json : "[]";
        }
        catch
        {
            return "[]";
        }
    }
}

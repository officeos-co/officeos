namespace OffceOs.Infrastructure.Features.Management;

internal sealed class OrganizationPolicyProfileRepository : IOrganizationPolicyProfileRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationPolicyProfileRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<OrganizationPolicyProfileRecord?> GetByAsync(OrganizationPolicyProfileFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.OrganizationPolicyProfiles.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(p => p.Id == filter.Id.Value);

        if (filter.OrganizationId.HasValue)
            query = query.Where(p => p.OrganizationId == filter.OrganizationId.Value);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<OrganizationPolicyProfileRecord> SaveAsync(OrganizationPolicyProfileRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrganizationPolicyProfiles
            .FirstOrDefaultAsync(p => p.OrganizationId == record.OrganizationId, ct);

        if (entity is null)
        {
            entity = ToEntity(record);
            _eaosDbContext.OrganizationPolicyProfiles.Add(entity);
        }
        else
        {
            entity.BrowserToolsEnabled = record.BrowserToolsEnabled;
            entity.NetworkToolsEnabled = record.NetworkToolsEnabled;
            entity.ShellToolsEnabled = record.ShellToolsEnabled;
            entity.FileWriteToolsEnabled = record.FileWriteToolsEnabled;
            entity.AllowedToolsJson = NormalizeJsonArray(record.AllowedToolsJson);
            entity.DeniedToolsJson = NormalizeJsonArray(record.DeniedToolsJson);
            entity.AllowedIntegrationsJson = NormalizeJsonArray(record.AllowedIntegrationsJson);
            entity.DeniedIntegrationsJson = NormalizeJsonArray(record.DeniedIntegrationsJson);
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(entity);
    }

    private static OrganizationPolicyProfileRecord ToRecord(OrganizationPolicyProfileEntity entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        BrowserToolsEnabled = entity.BrowserToolsEnabled,
        NetworkToolsEnabled = entity.NetworkToolsEnabled,
        ShellToolsEnabled = entity.ShellToolsEnabled,
        FileWriteToolsEnabled = entity.FileWriteToolsEnabled,
        AllowedToolsJson = entity.AllowedToolsJson,
        DeniedToolsJson = entity.DeniedToolsJson,
        AllowedIntegrationsJson = entity.AllowedIntegrationsJson,
        DeniedIntegrationsJson = entity.DeniedIntegrationsJson,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    private static OrganizationPolicyProfileEntity ToEntity(OrganizationPolicyProfileRecord record) => new()
    {
        Id = record.Id,
        OrganizationId = record.OrganizationId,
        BrowserToolsEnabled = record.BrowserToolsEnabled,
        NetworkToolsEnabled = record.NetworkToolsEnabled,
        ShellToolsEnabled = record.ShellToolsEnabled,
        FileWriteToolsEnabled = record.FileWriteToolsEnabled,
        AllowedToolsJson = NormalizeJsonArray(record.AllowedToolsJson),
        DeniedToolsJson = NormalizeJsonArray(record.DeniedToolsJson),
        AllowedIntegrationsJson = NormalizeJsonArray(record.AllowedIntegrationsJson),
        DeniedIntegrationsJson = NormalizeJsonArray(record.DeniedIntegrationsJson),
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

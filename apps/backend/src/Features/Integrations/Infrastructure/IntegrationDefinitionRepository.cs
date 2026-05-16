using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Integrations.Domain;
namespace OffceOs.Features.Integrations.Infrastructure;

internal sealed class IntegrationDefinitionRepository : IIntegrationDefinitionRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public IntegrationDefinitionRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Integrations.AsNoTracking()
            .Where(s => !s.IsBuiltin);

        if (workspaceId.HasValue)
            query = query.Where(s => s.WorkspaceId == workspaceId.Value);
        else
            query = query.Where(s => s.OwnerId == ownerId);

        var entities = await query.OrderBy(s => s.Category).ThenBy(s => s.Title).ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<IntegrationDefinitionRecord?> GetByNameAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Integrations.AsNoTracking()
            .Where(s => s.Name == name && !s.IsBuiltin);

        if (workspaceId.HasValue)
            query = query.Where(s => s.WorkspaceId == workspaceId.Value);
        else
            query = query.Where(s => s.OwnerId == ownerId);

        var entity = await query.FirstOrDefaultAsync(ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IntegrationDefinitionRecord> UpsertAsync(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Integrations
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name == server.Name && !s.IsBuiltin, ct);

        if (existing is null)
        {
            var entity = ToEntity(server);
            entity.Id = server.Id == Guid.Empty ? Guid.NewGuid() : server.Id;
            entity.OwnerId = ownerId;
            entity.WorkspaceId = workspaceId;
            entity.IsBuiltin = false;
            _eaosDbContext.Integrations.Add(entity);
            await _eaosDbContext.SaveChangesAsync(ct);
            return ToRecord(entity);
        }

        existing.Title = server.Title;
        existing.Provider = server.Provider;
        existing.Description = server.Description;
        existing.TransportType = server.TransportType.ToString();
        existing.Command = server.Command;
        existing.Args = server.Args;
        existing.Url = server.Url;
        existing.Logo = server.Logo;
        existing.Category = server.Category;
        existing.CredentialFieldsJson = server.CredentialFieldsJson;
        existing.Subtitle = server.Subtitle;
        existing.AuthorName = server.AuthorName;
        existing.AuthorUrl = server.AuthorUrl;
        existing.DocumentationUrl = server.DocumentationUrl;
        existing.RepositoryUrl = server.RepositoryUrl;
        existing.ToolsJson = SerializeTools(server.Tools);
        existing.CapabilitiesJson = server.CapabilitiesJson;
        existing.EntitiesJson = JsonSerializer.Serialize(server.Entities);

        await _eaosDbContext.SaveChangesAsync(ct);
        return ToRecord(existing);
    }

    public async Task DeleteAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Integrations
            .Where(s => s.Name == name && !s.IsBuiltin);

        if (workspaceId.HasValue)
            query = query.Where(s => s.WorkspaceId == workspaceId.Value);
        else
            query = query.Where(s => s.OwnerId == ownerId);

        await query.ExecuteDeleteAsync(ct);
    }

    private static IntegrationDefinitionRecord ToRecord(IntegrationDefinitionEntity entity) => new()
    {
        Id = entity.Id,
        OwnerId = entity.OwnerId,
        WorkspaceId = entity.WorkspaceId,
        Name = entity.Name,
        Provider = entity.Provider,
        Title = entity.Title,
        Description = entity.Description,
        TransportType = Enum.TryParse<IntegrationTransportType>(entity.TransportType, true, out var transport)
            ? transport
            : IntegrationTransportType.Stdio,
        Command = entity.Command,
        Args = entity.Args,
        Url = entity.Url,
        Logo = entity.Logo,
        Category = entity.Category,
        CredentialFieldsJson = entity.CredentialFieldsJson,
        Subtitle = entity.Subtitle,
        AuthorName = entity.AuthorName,
        AuthorUrl = entity.AuthorUrl,
        DocumentationUrl = entity.DocumentationUrl,
        RepositoryUrl = entity.RepositoryUrl,
        Tools = DeserializeTools(entity.ToolsJson),
        CapabilitiesJson = entity.CapabilitiesJson,
        Entities = DeserializeStringArray(entity.EntitiesJson),
        IsBuiltin = false,
        CreatedAt = entity.CreatedAt,
    };

    private static IntegrationDefinitionEntity ToEntity(IntegrationDefinitionRecord server) => new()
    {
        Id = server.Id,
        OwnerId = server.OwnerId,
        WorkspaceId = server.WorkspaceId,
        Name = server.Name,
        Provider = server.Provider,
        Title = server.Title,
        Description = server.Description,
        TransportType = server.TransportType.ToString(),
        Command = server.Command,
        Args = server.Args,
        Url = server.Url,
        Logo = server.Logo,
        Category = server.Category,
        CredentialFieldsJson = server.CredentialFieldsJson,
        Subtitle = server.Subtitle,
        AuthorName = server.AuthorName,
        AuthorUrl = server.AuthorUrl,
        DocumentationUrl = server.DocumentationUrl,
        RepositoryUrl = server.RepositoryUrl,
        ToolsJson = SerializeTools(server.Tools),
        CapabilitiesJson = server.CapabilitiesJson,
        EntitiesJson = JsonSerializer.Serialize(server.Entities),
        IsBuiltin = false,
        CreatedAt = server.CreatedAt,
    };

    private static IReadOnlyList<string> DeserializeStringArray(string? json)
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

    private static IReadOnlyList<IntegrationCatalogToolRecord> DeserializeTools(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            if (parsed.ValueKind != JsonValueKind.Array)
                return [];

            var tools = new List<IntegrationCatalogToolRecord>();
            foreach (var item in parsed.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var description = item.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? name
                    : name;
                object? parameters = null;
                if (item.TryGetProperty("parameters", out var parametersProp)
                    || item.TryGetProperty("inputSchema", out parametersProp)
                    || item.TryGetProperty("input_schema", out parametersProp))
                {
                    parameters = JsonSerializer.Deserialize<JsonElement>(parametersProp.GetRawText());
                }

                tools.Add(new IntegrationCatalogToolRecord(name, description, parameters));
            }
            return tools;
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeTools(IReadOnlyList<IntegrationCatalogToolRecord> tools) =>
        JsonSerializer.Serialize(tools.Select(tool => new
        {
            name = tool.Name,
            description = tool.Description,
            parameters = tool.Parameters,
        }));
}

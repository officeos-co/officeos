using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class McpServerRepository : IMcpServerRepository
{
    private readonly EaosDbContext _db;

    public McpServerRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _db.McpServers.AsNoTracking()
            .Where(s => !s.IsBuiltin)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Title)
            .ToListAsync(ct);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<McpServerRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var entity = await _db.McpServers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name && !s.IsBuiltin, ct);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<McpServerRecord> UpsertAsync(McpServerRecord server, CancellationToken ct = default)
    {
        var existing = await _db.McpServers
            .FirstOrDefaultAsync(s => s.Name == server.Name && !s.IsBuiltin, ct);

        if (existing is null)
        {
            var entity = ToEntity(server);
            entity.Id = server.Id == Guid.Empty ? Guid.NewGuid() : server.Id;
            entity.IsBuiltin = false;
            _db.McpServers.Add(entity);
            await _db.SaveChangesAsync(ct);
            return ToRecord(entity);
        }

        existing.Title = server.Title;
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
        existing.ToolsJson = server.ToolsJson;

        await _db.SaveChangesAsync(ct);
        return ToRecord(existing);
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        await _db.McpServers
            .Where(s => s.Name == name && !s.IsBuiltin)
            .ExecuteDeleteAsync(ct);
    }

    private static McpServerRecord ToRecord(McpServerEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Title = entity.Title,
        Description = entity.Description,
        TransportType = Enum.TryParse<McpTransportType>(entity.TransportType, true, out var transport)
            ? transport
            : McpTransportType.Stdio,
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
        ToolsJson = entity.ToolsJson,
        IsBuiltin = false,
        CreatedAt = entity.CreatedAt,
    };

    private static McpServerEntity ToEntity(McpServerRecord server) => new()
    {
        Id = server.Id,
        Name = server.Name,
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
        ToolsJson = server.ToolsJson,
        IsBuiltin = false,
        CreatedAt = server.CreatedAt,
    };
}

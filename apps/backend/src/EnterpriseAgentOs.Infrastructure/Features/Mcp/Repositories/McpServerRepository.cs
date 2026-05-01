using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class McpServerRepository : IMcpServerRepository
{
    private readonly EaosDbContext _db;

    public McpServerRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<McpServerRecord>> ListAsync(CancellationToken ct)
    {
        var entities = await _db.McpServers.AsNoTracking()
            .OrderBy(s => s.Name).ToListAsync(ct);
        return entities.Select(ToRecord).ToList();
    }

    public async Task<McpServerRecord?> GetByNameAsync(string name, CancellationToken ct)
    {
        var entity = await _db.McpServers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<McpServerRecord> UpsertAsync(McpServerRecord server, CancellationToken ct)
    {
        var existing = await _db.McpServers.FirstOrDefaultAsync(s => s.Name == server.Name, ct);
        if (existing is not null)
        {
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
            existing.IsBuiltin = server.IsBuiltin;
        }
        else
        {
            _db.McpServers.Add(ToEntity(server));
        }
        await _db.SaveChangesAsync(ct);
        return server;
    }

    public async Task DeleteAsync(string name, CancellationToken ct)
    {
        await _db.McpServers.Where(s => s.Name == name).ExecuteDeleteAsync(ct);
    }

    private static McpServerRecord ToRecord(McpServerEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Title = e.Title,
        Description = e.Description,
        TransportType = Enum.TryParse<McpTransportType>(e.TransportType, out var t) ? t : McpTransportType.Stdio,
        Command = e.Command,
        Args = e.Args,
        Url = e.Url,
        Logo = e.Logo,
        Category = e.Category,
        CredentialFieldsJson = e.CredentialFieldsJson,
        Subtitle = e.Subtitle,
        AuthorName = e.AuthorName,
        AuthorUrl = e.AuthorUrl,
        DocumentationUrl = e.DocumentationUrl,
        RepositoryUrl = e.RepositoryUrl,
        ToolsJson = e.ToolsJson,
        IsBuiltin = e.IsBuiltin,
        CreatedAt = e.CreatedAt,
    };

    private static McpServerEntity ToEntity(McpServerRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Title = r.Title,
        Description = r.Description,
        TransportType = r.TransportType.ToString(),
        Command = r.Command,
        Args = r.Args,
        Url = r.Url,
        Logo = r.Logo,
        Category = r.Category,
        CredentialFieldsJson = r.CredentialFieldsJson,
        Subtitle = r.Subtitle,
        AuthorName = r.AuthorName,
        AuthorUrl = r.AuthorUrl,
        DocumentationUrl = r.DocumentationUrl,
        RepositoryUrl = r.RepositoryUrl,
        ToolsJson = r.ToolsJson,
        IsBuiltin = r.IsBuiltin,
        CreatedAt = r.CreatedAt,
    };
}

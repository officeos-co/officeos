using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.AgentMemory;

[ApiController]
[Route("api/agents/me")]
[AgentTokenAuth]
public sealed class AgentMemoryController : ControllerBase
{
    private readonly EaosDbContext _db;

    public AgentMemoryController(EaosDbContext db) => _db = db;

    private Guid AgentId => (Guid)HttpContext.Items["agent-id"]!;

    // ── Memory ───────────────────────────────────────────────────────

    [HttpPost("memory")]
    public async Task<IActionResult> StoreMemory([FromBody] StoreMemoryRequest req, CancellationToken ct)
    {
        // Upsert by (AgentId, Key)
        var existing = await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == AgentId && m.Key == req.Key, ct);

        if (existing is not null)
        {
            existing.Content = req.Content;
            existing.Category = req.Category ?? "core";
            existing.Namespace = req.Namespace ?? "default";
            existing.SessionId = req.SessionId;
            existing.Importance = req.Importance;
        }
        else
        {
            _db.AgentMemories.Add(new AgentMemoryRecord
            {
                AgentId = AgentId,
                Key = req.Key,
                Content = req.Content,
                Category = req.Category ?? "core",
                Namespace = req.Namespace ?? "default",
                SessionId = req.SessionId,
                Importance = req.Importance,
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpGet("memory/search")]
    public async Task<IActionResult> SearchMemory(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] string? sessionId = null,
        [FromQuery] string? since = null,
        [FromQuery] string? until = null,
        CancellationToken ct = default)
    {
        var q = _db.AgentMemories
            .AsNoTracking()
            .Where(m => m.AgentId == AgentId);

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(m => EF.Functions.ILike(m.Content, $"%{query}%")
                          || EF.Functions.ILike(m.Key, $"%{query}%"));

        if (!string.IsNullOrWhiteSpace(sessionId))
            q = q.Where(m => m.SessionId == sessionId);

        if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var sinceDate))
            q = q.Where(m => m.CreatedAt >= sinceDate);

        if (!string.IsNullOrWhiteSpace(until) && DateTime.TryParse(until, out var untilDate))
            q = q.Where(m => m.CreatedAt <= untilDate);

        var entries = await q
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

        return Ok(entries.Select(ToDto));
    }

    [HttpGet("memory")]
    public async Task<IActionResult> ListMemory(
        [FromQuery] string? category = null,
        [FromQuery] string? sessionId = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var q = _db.AgentMemories
            .AsNoTracking()
            .Where(m => m.AgentId == AgentId);

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(m => m.Category == category);

        if (!string.IsNullOrWhiteSpace(sessionId))
            q = q.Where(m => m.SessionId == sessionId);

        var entries = await q
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        return Ok(entries.Select(ToDto));
    }

    [HttpGet("memory/{key}")]
    public async Task<IActionResult> GetMemory(string key, CancellationToken ct)
    {
        var entry = await _db.AgentMemories
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.AgentId == AgentId && m.Key == key, ct);

        if (entry is null) return NotFound();
        return Ok(ToDto(entry));
    }

    [HttpDelete("memory/{key}")]
    public async Task<IActionResult> DeleteMemory(string key, CancellationToken ct)
    {
        var count = await _db.AgentMemories
            .Where(m => m.AgentId == AgentId && m.Key == key)
            .ExecuteDeleteAsync(ct);

        return count > 0 ? Ok(new { deleted = true }) : NotFound();
    }

    // ── Response cache ───────────────────────────────────────────────

    [HttpPost("cache")]
    public async Task<IActionResult> StoreCache([FromBody] StoreCacheRequest req, CancellationToken ct)
    {
        // Upsert by (AgentId, CacheKey)
        var existing = await _db.AgentCacheEntries
            .FirstOrDefaultAsync(c => c.AgentId == AgentId && c.CacheKey == req.CacheKey, ct);

        if (existing is not null)
        {
            existing.Response = req.Response;
            existing.Model = req.Model ?? "";
            existing.TokenCount = req.TokenCount;
            existing.ExpiresAt = req.ExpiresAt;
            existing.AccessedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AgentCacheEntries.Add(new AgentCacheRecord
            {
                AgentId = AgentId,
                CacheKey = req.CacheKey,
                Model = req.Model ?? "",
                Response = req.Response,
                TokenCount = req.TokenCount,
                ExpiresAt = req.ExpiresAt,
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpGet("cache/{cacheKey}")]
    public async Task<IActionResult> GetCache(string cacheKey, CancellationToken ct)
    {
        var entry = await _db.AgentCacheEntries
            .FirstOrDefaultAsync(c => c.AgentId == AgentId && c.CacheKey == cacheKey, ct);

        if (entry is null) return NotFound();

        // Check expiry
        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
        {
            _db.AgentCacheEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
            return NotFound();
        }

        // Touch access time
        entry.AccessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            entry.CacheKey,
            entry.Model,
            entry.Response,
            entry.TokenCount,
            entry.CreatedAt,
            entry.ExpiresAt,
        });
    }

    // ── Conversations ────────────────────────────────────────────────

    [HttpPost("conversations")]
    public async Task<IActionResult> StoreConversation([FromBody] StoreConversationRequest req, CancellationToken ct)
    {
        var type = req.Role switch
        {
            "user" => AgentLogType.MessageIn,
            "assistant" => AgentLogType.MessageOut,
            _ => AgentLogType.System,
        };

        _db.AgentLogs.Add(new AgentLogRecord
        {
            AgentId = AgentId,
            Type = type,
            Content = req.Content,
            CorrelationId = req.SessionId,
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> ListConversations(
        [FromQuery] int limit = 50,
        [FromQuery] string? sessionId = null,
        CancellationToken ct = default)
    {
        var q = _db.AgentLogs
            .AsNoTracking()
            .Where(c => c.AgentId == AgentId &&
                (c.Type == AgentLogType.MessageIn ||
                 c.Type == AgentLogType.MessageOut ||
                 c.Type == AgentLogType.System));

        if (!string.IsNullOrWhiteSpace(sessionId))
            q = q.Where(c => c.CorrelationId == sessionId);

        var entries = await q
            .OrderByDescending(c => c.Time)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        return Ok(entries.Select(c => new
        {
            c.Id,
            Role = c.Type == AgentLogType.MessageIn ? "user"
                : c.Type == AgentLogType.MessageOut ? "assistant"
                : "system",
            c.Content,
            SessionId = c.CorrelationId,
            ToolCalls = (JsonElement?)null,
            CreatedAt = c.Time,
        }));
    }

    // ── DTOs ─────────────────────────────────────────────────────────

    private static object ToDto(AgentMemoryRecord m) => new
    {
        m.Id,
        m.Key,
        m.Content,
        m.Category,
        m.Namespace,
        m.SessionId,
        m.Importance,
        m.SupersededBy,
        m.CreatedAt,
    };
}

public sealed record StoreMemoryRequest
{
    public required string Key { get; init; }
    public required string Content { get; init; }
    public string? Category { get; init; }
    public string? Namespace { get; init; }
    public string? SessionId { get; init; }
    public double? Importance { get; init; }
}

public sealed record StoreCacheRequest
{
    public required string CacheKey { get; init; }
    public required string Response { get; init; }
    public string? Model { get; init; }
    public int TokenCount { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed record StoreConversationRequest
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public string? SessionId { get; init; }
    public JsonElement[]? ToolCalls { get; init; }
}

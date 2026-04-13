using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Channels;

[ApiController]
[Route("api/channels")]
public sealed class ChannelController : ControllerBase
{
    private readonly IChannelRepository _repo;
    private readonly ChannelConfigProtector _protector;

    public ChannelController(IChannelRepository repo, ChannelConfigProtector protector)
    {
        _repo = repo;
        _protector = protector;
    }

    /// <summary>List supported channel types with their config schemas.</summary>
    [HttpGet("types")]
    public ActionResult<IReadOnlyList<ChannelTypeDto>> ListTypes()
    {
        var types = ChannelTypes.All.Select(t => new ChannelTypeDto(
            t.Type, t.DisplayName, t.Description, t.ConfigFields)).ToList();
        return Ok(types);
    }

    /// <summary>List all org channel connections.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChannelConnectionDto>>> List(CancellationToken ct)
    {
        var records = await _repo.ListConnectionsAsync(ct);
        return Ok(records.Select(ToDto).ToList());
    }

    /// <summary>Create a new channel connection.</summary>
    [HttpPost]
    public async Task<ActionResult<ChannelConnectionDto>> Create(
        [FromBody] CreateChannelConnectionRequest request,
        CancellationToken ct)
    {
        var typeDef = ChannelTypes.GetByType(request.ChannelType);
        if (typeDef is null)
            return BadRequest(new { error = $"Unknown channel type: {request.ChannelType}" });

        // Validate required config fields
        var requiredFields = typeDef.ConfigFields.Where(f => f.Required).ToList();
        foreach (var field in requiredFields)
        {
            if (request.Config is null || !request.Config.ContainsKey(field.Key) ||
                string.IsNullOrWhiteSpace(request.Config[field.Key]))
            {
                return BadRequest(new { error = $"Missing required config field: {field.Key}" });
            }
        }

        string? encryptedConfig = null;
        if (request.Config is not null && request.Config.Count > 0)
        {
            var json = JsonSerializer.Serialize(request.Config);
            encryptedConfig = _protector.Protect(json);
        }

        var user = HttpContext.Items["User"] as UserRecord;

        var record = new ChannelConnectionRecord
        {
            ChannelType = request.ChannelType.ToLowerInvariant(),
            DisplayName = request.DisplayName,
            EncryptedConfig = encryptedConfig,
            CreatedById = user?.Id,
        };

        var created = await _repo.CreateConnectionAsync(record, ct);
        return Ok(ToDto(created));
    }

    /// <summary>Update a channel connection.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChannelConnectionDto>> Update(
        Guid id,
        [FromBody] UpdateChannelConnectionRequest request,
        CancellationToken ct)
    {
        var updated = await _repo.UpdateConnectionAsync(id, row =>
        {
            if (request.DisplayName is not null)
                row.DisplayName = request.DisplayName;
            if (request.Enabled.HasValue)
                row.Enabled = request.Enabled.Value;
            if (request.Config is not null && request.Config.Count > 0)
            {
                var json = JsonSerializer.Serialize(request.Config);
                row.EncryptedConfig = _protector.Protect(json);
            }
        }, ct);

        return updated is null ? NotFound() : Ok(ToDto(updated));
    }

    /// <summary>Delete a channel connection and all its agent bindings.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteConnectionAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private static ChannelConnectionDto ToDto(ChannelConnectionRecord r) => new(
        r.Id,
        r.ChannelType,
        r.DisplayName,
        r.Enabled,
        !string.IsNullOrEmpty(r.EncryptedConfig),
        r.CreatedAt,
        r.CreatedById);
}

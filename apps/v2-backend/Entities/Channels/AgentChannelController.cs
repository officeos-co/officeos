using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Channels;

[ApiController]
[Route("api/agents/{agentId:guid}/channels")]
public sealed class AgentChannelController : ControllerBase
{
    private readonly IChannelRepository _repo;

    public AgentChannelController(IChannelRepository repo)
    {
        _repo = repo;
    }

    /// <summary>List all channel bindings for an agent.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentChannelBindingDto>>> List(
        Guid agentId, CancellationToken ct)
    {
        var bindings = await _repo.ListBindingsAsync(agentId, ct);
        return Ok(bindings.Select(ToDto).ToList());
    }

    /// <summary>Bind an agent to an org channel connection.</summary>
    [HttpPost]
    public async Task<ActionResult<AgentChannelBindingDto>> Create(
        Guid agentId,
        [FromBody] CreateAgentChannelBindingRequest request,
        CancellationToken ct)
    {
        // Verify the connection exists
        var connection = await _repo.GetConnectionAsync(request.ChannelConnectionId, ct);
        if (connection is null)
            return BadRequest(new { error = "Channel connection not found" });

        var record = new AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = request.ChannelConnectionId,
            Config = request.Config is not null
                ? JsonSerializer.Serialize(request.Config)
                : null,
        };

        try
        {
            var created = await _repo.CreateBindingAsync(record, ct);
            return Ok(ToDto(created));
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Agent is already bound to this channel connection" });
        }
    }

    /// <summary>Update a channel binding's config or enabled state.</summary>
    [HttpPut("{bindingId:guid}")]
    public async Task<ActionResult<AgentChannelBindingDto>> Update(
        Guid agentId,
        Guid bindingId,
        [FromBody] UpdateAgentChannelBindingRequest request,
        CancellationToken ct)
    {
        var updated = await _repo.UpdateBindingAsync(bindingId, row =>
        {
            if (row.AgentId != agentId) return; // Guard: binding must belong to this agent

            if (request.Enabled.HasValue)
                row.Enabled = request.Enabled.Value;
            if (request.Config is not null)
                row.Config = JsonSerializer.Serialize(request.Config);
        }, ct);

        if (updated is null) return NotFound();
        if (updated.AgentId != agentId) return NotFound();

        return Ok(ToDto(updated));
    }

    /// <summary>Unbind an agent from a channel.</summary>
    [HttpDelete("{bindingId:guid}")]
    public async Task<ActionResult> Delete(
        Guid agentId, Guid bindingId, CancellationToken ct)
    {
        // Verify binding belongs to this agent
        var binding = await _repo.GetBindingAsync(bindingId, ct);
        if (binding is null || binding.AgentId != agentId) return NotFound();

        await _repo.DeleteBindingAsync(bindingId, ct);
        return NoContent();
    }

    private static AgentChannelBindingDto ToDto(AgentChannelBindingRecord r) => new(
        r.Id,
        r.AgentId,
        r.ChannelConnectionId,
        r.ChannelConnection?.ChannelType ?? "",
        r.ChannelConnection?.DisplayName ?? "",
        r.Enabled,
        string.IsNullOrEmpty(r.Config)
            ? null
            : JsonSerializer.Deserialize<AgentChannelConfig>(r.Config,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
        r.CreatedAt);
}

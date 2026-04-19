using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Application.Services.AgentTemplates;

public sealed class AgentTemplateService : IAgentTemplateService
{
    private readonly IAgentTemplateRepository _repo;
    private readonly IAgentService _agents;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly IChannelRepository _channels;
    private readonly IPostHogService _analytics;
    private readonly ILogger<AgentTemplateService> _logger;

    public AgentTemplateService(
        IAgentTemplateRepository repo,
        IAgentService agents,
        IAgentSkillRepository agentSkills,
        IChannelRepository channels,
        IPostHogService analytics,
        ILogger<AgentTemplateService> logger)
    {
        _repo = repo;
        _agents = agents;
        _agentSkills = agentSkills;
        _channels = channels;
        _analytics = analytics;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentTemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _repo.ListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<AgentTemplateDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _repo.GetAsync(id, ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<AgentDto> CreateAgentFromTemplateAsync(
        Guid templateId,
        string name,
        string provider,
        string? model,
        Guid ownerId,
        CancellationToken ct = default)
    {
        var template = await _repo.GetAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Template '{templateId}' not found.");

        var dto = ToDto(template);

        var agent = await _agents.CreateAsync(
            new CreateAgentRequest(name, provider, model, template.Prompt),
            ownerId: ownerId,
            ct);

        if (dto.Integrations.Count > 0)
        {
            await _agentSkills.AssignAsync(agent.Id, dto.Integrations, ct);
        }

        if (dto.Channels.Count > 0)
        {
            var connections = await _channels.ListConnectionsAsync(ct);
            foreach (var slug in dto.Channels)
            {
                var match = connections.FirstOrDefault(c =>
                    string.Equals(c.ChannelType, slug, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue;
                try
                {
                    await _channels.CreateBindingAsync(new AgentChannelBindingRecord
                    {
                        AgentId = agent.Id,
                        ChannelConnectionId = match.Id,
                    }, ct);
                }
                catch (DbUpdateException)
                {
                    // already bound — skip
                }
            }
        }

        _logger.LogInformation("Created agent {AgentId} from template {Template}", agent.Id, template.Name);

        await _analytics.CaptureAsync(
            ownerId.ToString(),
            "agent_created_from_template",
            new Dictionary<string, object?>
            {
                ["template_id"] = template.Id,
                ["template_name"] = template.Name,
                ["agent_id"] = agent.Id,
            },
            ct);

        return agent;
    }

    internal static AgentTemplateDto ToDto(AgentTemplateRecord record)
    {
        var integrations = Deserialize(record.IntegrationsJson);
        var channels = Deserialize(record.ChannelsJson);
        return new AgentTemplateDto(
            record.Id,
            record.Name,
            record.Description,
            record.Prompt,
            integrations,
            channels,
            record.IsBuiltin);
    }

    private static IReadOnlyList<string> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

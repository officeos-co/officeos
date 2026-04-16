using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.AgentTemplates;

public sealed class AgentTemplateService : IAgentTemplateService
{
    private readonly IAgentTemplateRepository _repo;
    private readonly IAgentService _agents;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly ILogger<AgentTemplateService> _logger;

    public AgentTemplateService(
        IAgentTemplateRepository repo,
        IAgentService agents,
        IAgentSkillRepository agentSkills,
        ILogger<AgentTemplateService> logger)
    {
        _repo = repo;
        _agents = agents;
        _agentSkills = agentSkills;
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

        // TODO Stage 5b: bind agent to template.Channels once IChannelRepository
        // exposes a slug-based BindAsync(agentId, slug) helper. Current repo only
        // supports binding via a pre-existing ChannelConnectionId.
        _ = dto.Channels;

        _logger.LogInformation("Created agent {AgentId} from template {Template}", agent.Id, template.Name);
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

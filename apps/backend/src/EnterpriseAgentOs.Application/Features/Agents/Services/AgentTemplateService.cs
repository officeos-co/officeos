namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentTemplateService : IAgentTemplateService
{
    private readonly IAgentTemplateRepository _agentTemplateRepository;
    private readonly IAgentService _agentService;
    private readonly AgentChannelBinder _channelBinder;
    private readonly IPostHogService _postHogService;
    private readonly ILogger<AgentTemplateService> _logger;

    public AgentTemplateService(
        IAgentTemplateRepository repo,
        IAgentService agents,
        AgentChannelBinder channelBinder,
        IPostHogService analytics,
        ILogger<AgentTemplateService> logger)
    {
        _agentTemplateRepository = repo;
        _agentService = agents;
        _channelBinder = channelBinder;
        _postHogService = analytics;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentTemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _agentTemplateRepository.ListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<AgentTemplateDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _agentTemplateRepository.GetAsync(id, ct);
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
        var template = await _agentTemplateRepository.GetAsync(templateId, ct)
            ?? throw new InvalidOperationException($"Template '{templateId}' not found.");

        var dto = ToDto(template);

        var agent = await _agentService.CreateAsync(
            new CreateAgentRequest(name, provider, model, template.Prompt),
            ownerId: ownerId,
            ct);

        await _channelBinder.BindBySlugsAsync(agent.Id, dto.Channels, ct);

        _logger.LogInformation("Created agent {AgentId} from template {Template}", agent.Id, template.Name);

        await _postHogService.CaptureAsync(
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

using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDeployer _agentDeployer;
    private readonly IProviderService _providerService;
    private readonly ILogger<AgentService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IAgentPersonalityRepository _agentPersonalityRepository;
    private readonly IPublisher _publisher;
    private readonly AgentChannelBinder _channelBinder;
    private readonly IAgentLogService _agentLogService;
    private readonly IMcpServerService _mcpServerService;
    private readonly IAgentToolPermissionRepository _toolPermissionRepository;

    private static readonly TimeSpan AgentCacheTtl = TimeSpan.FromSeconds(30);
    private static string AgentListCacheKey(AgentFilter filter)
        => $"agents:list:id={filter.Id?.ToString() ?? "all"}:owner={filter.OwnerId?.ToString() ?? "all"}:deleted={filter.IncludeDeleted}";
    private static string AgentCacheKey(AgentFilter filter)
        => $"agents:detail:id={filter.Id?.ToString() ?? "any"}:owner={filter.OwnerId?.ToString() ?? "any"}:deleted={filter.IncludeDeleted}";

    public AgentService(
        IAgentRepository repository,
        IAgentDeployer deployer,
        IProviderService providerService,
        ILogger<AgentService> logger,
        IDistributedCache cache,
        IAgentPersonalityRepository personalityRepo,
        IPublisher publisher,
        AgentChannelBinder channelBinder,
        IAgentLogService agentLogService,
        IMcpServerService mcpServerService,
        IAgentToolPermissionRepository toolPermissionRepository)
    {
        _agentRepository = repository;
        _agentDeployer = deployer;
        _providerService = providerService;
        _logger = logger;
        _cache = cache;
        _agentPersonalityRepository = personalityRepo;
        _publisher = publisher;
        _channelBinder = channelBinder;
        _agentLogService = agentLogService;
        _mcpServerService = mcpServerService;
        _toolPermissionRepository = toolPermissionRepository;
    }

    public async Task<IReadOnlyList<AgentDto>> ListAsync(AgentFilter filter, CancellationToken ct = default)
    {
        var cacheKey = AgentListCacheKey(filter);
        var cached = await _cache.GetJsonAsync<IReadOnlyList<AgentDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var records = await _agentRepository.ListAsync(filter, ct);
        _logger.LogDebug("Listing {Count} agents, refreshing pod status", records.Count);
        await Task.WhenAll(records
            .Where(r => !string.IsNullOrEmpty(r.PodName))
            .Select(r => RefreshStatusAsync(r, ct)));
        var result = records.Select(ToDto).ToList();

        await _cache.SetJsonAsync(cacheKey, (IReadOnlyList<AgentDto>)result, AgentCacheTtl, ct);
        return result;
    }

    public async Task<AgentDto?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
    {
        var key = AgentCacheKey(filter);
        var cached = await _cache.GetJsonAsync<AgentDto>(key, ct);
        if (cached is not null)
            return cached;

        var record = await _agentRepository.GetByAsync(filter, ct);
        if (record is null)
        {
            _logger.LogDebug("Agent not found for filter {@Filter}", filter);
            return null;
        }
        await RefreshStatusAsync(record, ct);
        var dto = ToDto(record);

        await _cache.SetJsonAsync(key, dto, AgentCacheTtl, ct);
        return dto;
    }

    public async Task<AgentDto> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating agent {AgentName} with provider {Provider} model {Model}",
            request.Name, request.Provider, request.Model);

        var apiKey = await _providerService.GetApiKeyForDispatchAsync(request.Provider, ct);
        if (apiKey is null)
        {
            throw new InvalidOperationException(
                $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
        }

        var record = AgentRecord.Create(request.Name, request.Provider, request.Model, ownerId, request.Prompt);

        await _agentRepository.AddAsync(record, ct);

        // Seed default personality files — domain owns the content and validation.
        var defaults = AgentPersonalityRecord.CreateDefaults(record.Id, record.Name);
        foreach (var personality in defaults)
            await _agentPersonalityRepository.UpsertAsync(record.Id, personality.FileName, personality.Content, ct);

        // If the user supplied a system prompt, persist it as BOOTSTRAP.md so
        // the agent's prompt composition includes it on every turn.

        //TODO should be inserted into bootstrap put that logic into domain not replaced entirely
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            await _agentPersonalityRepository.UpsertAsync(
                record.Id, "BOOTSTRAP.md", request.Prompt.Trim(), ct);
        }

        _logger.LogInformation("Agent {AgentId} record created: {AgentName} ({Provider}/{Model})",
            record.Id, record.Name, record.Provider, record.Model);

        await _publisher.Publish(new AgentCreatedEvent(record.Id, record.Provider, record.Model, ownerId), ct);

        return ToDto(record);
    }

    public async Task<AgentDto?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default)
    {
        var record = await _agentRepository.GetByAsync(new AgentFilter { Id = id }, ct);
        if (record is null)
        {
            _logger.LogWarning("Patch failed: agent {AgentId} not found", id);
            return null;
        }
        _logger.LogInformation("Patching agent {AgentId}: Provider={Provider} Model={Model}",
            id, request.Provider, request.Model);

        if (!string.IsNullOrWhiteSpace(request.Provider))
        {
            var provider = request.Provider.Trim().ToLowerInvariant();
            var key = await _providerService.GetApiKeyForDispatchAsync(provider, ct);
            if (key is null)
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' is not configured. Set its API key on the Providers page first.");
            }
            record.Provider = provider;
        }

        if (request.Model is not null)
        {
            record.ValidateAndSetModel(request.Model);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            record.Name = request.Name.Trim();
        }

        if (request.Prompt is not null)
        {
            record.Prompt = request.Prompt.Length == 0 ? null : request.Prompt;
        }

        await _agentRepository.UpdateAsync(record, ct);
        await _publisher.Publish(new AgentUpdatedEvent(id), ct);
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _agentRepository.GetByAsync(new AgentFilter { Id = id }, ct);
        if (record is null)
        {
            _logger.LogWarning("Delete failed: agent {AgentId} not found", id);
            return false;
        }

        _logger.LogInformation("Deleting agent {AgentId} ({AgentName})", id, record.Name);

        var deleted = await _agentRepository.SoftDeleteAsync(new AgentFilter { Id = id }, ct);

        if (deleted)
            await _publisher.Publish(new AgentDeletedEvent(id, record.PodName, record.HasPod, record.OwnerId), ct);

        return deleted;
    }

    public async Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default)
    {
        await _channelBinder.BindBySlugsAsync(agentId, init.ChannelSlugs, ct);

        if (init.ToolNames is { Count: > 0 })
        {
            var servers = await _mcpServerService.ListAsync(ct);
            var names = servers.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var toolName in init.ToolNames)
            {
                var parsed = ToolKey.Parse(toolName);
                var serverName = names.Contains(toolName) ? toolName : parsed.SkillName;
                if (names.Contains(serverName))
                    await _mcpServerService.AssignToAgentAsync(agentId, serverName, ct);
            }
        }

        if (init.ToolPermissions is { Count: > 0 })
        {
            foreach (var permission in init.ToolPermissions)
            {
                var key = AgentToolPermissionResolver.NormalizeDashboardKey(permission.Tool);
                await _toolPermissionRepository.UpsertAsync(agentId, key.SkillName, key.ToolName, permission.Mode, ct);
            }
        }

        // Bootstrap message
        if (!string.IsNullOrWhiteSpace(init.BootstrapMessage))
        {
            await _agentLogService.SendMessageAsync(agentId, init.BootstrapMessage, userId, ct);
        }
    }

    private async Task RefreshStatusAsync(AgentRecord record, CancellationToken ct)
    {
        if (!record.HasPod || string.IsNullOrEmpty(record.PodName)) return;
        try
        {
            var live = await _agentDeployer.GetStatusAsync(record.PodName, ct);
            var liveStatus = live.ToAgentStatus();
            if (liveStatus != record.Status)
            {
                await _agentRepository.UpdateStatusAsync(new AgentFilter { Id = record.Id }, liveStatus, ct);
                record.Status = liveStatus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh status for agent {AgentId}", record.Id);
        }
    }

    private static AgentDto ToDto(AgentRecord record) =>
        new(record.Id, record.Name, record.Provider, record.Model, record.Prompt, record.Status.ToStorageString(),
            record.PodName, record.ServiceUrl, record.CreatedAt);
}

namespace OffceOs.Application.Features.Agents;

internal sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDeployer _agentDeployer;
    private readonly IProviderService _providerService;
    private readonly ILogger<AgentService> _logger;
    private readonly IDistributedCache _distributedCache;
    private readonly IAgentPersonalityRepository _agentPersonalityRepository;
    private readonly IPublisher _publisher;
    private readonly AgentChannelBinder _agentChannelBinder;
    private readonly IAgentLogService _agentLogService;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IAgentToolPermissionRepository _agentToolPermissionRepository;

    private static readonly TimeSpan AgentCacheTtl = TimeSpan.FromSeconds(30);
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
        IIntegrationDefinitionService integrationDefinitionService,
        IAgentToolPermissionRepository toolPermissionRepository)
    {
        _agentRepository = repository;
        _agentDeployer = deployer;
        _providerService = providerService;
        _logger = logger;
        _distributedCache = cache;
        _agentPersonalityRepository = personalityRepo;
        _publisher = publisher;
        _agentChannelBinder = channelBinder;
        _agentLogService = agentLogService;
        _integrationDefinitionService = integrationDefinitionService;
        _agentToolPermissionRepository = toolPermissionRepository;
    }

    public async Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default)
    {
        var cacheKey = AgentCacheKeys.List(filter);
        var cached = await _distributedCache.GetJsonAsync<IReadOnlyList<AgentRecord>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var records = await _agentRepository.ListAsync(filter, ct);
        _logger.LogDebug("Listing {Count} agents, refreshing pod status", records.Count);
        await Task.WhenAll(records
            .Where(r => !string.IsNullOrEmpty(r.PodName))
            .Select(r => RefreshStatusAsync(r, ct)));
        await _distributedCache.SetJsonAsync(cacheKey, records, AgentCacheTtl, ct);
        await AgentCacheKeys.TrackListAsync(_distributedCache, cacheKey, ct);
        return records;
    }

    public async Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default)
    {
        var key = AgentCacheKeys.Detail(filter);
        var cached = await _distributedCache.GetJsonAsync<AgentRecord>(key, ct);
        if (cached is not null)
            return cached;

        var record = await _agentRepository.GetByAsync(filter, ct);
        if (record is null)
        {
            _logger.LogDebug("Agent not found for filter {@Filter}", filter);
            return null;
        }
        await RefreshStatusAsync(record, ct);

        await _distributedCache.SetJsonAsync(key, record, AgentCacheTtl, ct);
        return record;
    }

    public async Task<AgentRecord> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating agent {AgentName} with provider {Provider} model {Model}",
            request.Name, request.Provider, request.Model);

        if (await RequiresConfiguredProviderKeyAsync(request.Provider, workspaceId, ct))
        {
            throw new InvalidOperationException(
                $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
        }

        if (!await _providerService.IsModelAllowedAsync(request.Provider, request.Model, workspaceId, ct))
            throw new InvalidOperationException($"Model '{request.Model ?? ProviderRegistry.DefaultModel}' is not allowed for provider '{request.Provider}'.");

        var record = AgentRecord.Create(request.Name, request.Provider, request.Model, ownerId, request.Prompt, workspaceId);

        await _agentRepository.AddAsync(record, ct);

        // Seed default personality files — domain owns the content and validation.
        var defaults = AgentPersonalityRecord.CreateDefaults(record.Id, record.Name);
        foreach (var personality in defaults)
            await _agentPersonalityRepository.UpsertAsync(record.Id, personality.FileName, personality.Content, ct);

        // If the user supplied a system prompt, merge it into BOOTSTRAP.md
        // while preserving the domain-owned default bootstrap guidance.
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            await _agentPersonalityRepository.UpsertAsync(
                record.Id, "BOOTSTRAP.md", AgentPersonalityRecord.CreateBootstrapContent(request.Prompt), ct);
        }

        _logger.LogInformation("Agent {AgentId} record created: {AgentName} ({Provider}/{Model})",
            record.Id, record.Name, record.Provider, record.Model);

        await _publisher.Publish(new AgentCreatedEvent(record.Id, record.Provider, record.Model, ownerId), ct);

        return record;
    }

    public async Task<AgentRecord?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default)
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
            if (await RequiresConfiguredProviderKeyAsync(provider, record.WorkspaceId, ct))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' is not configured. Set its API key on the Providers page first.");
            }
            if (!await _providerService.IsModelAllowedAsync(provider, request.Model ?? record.Model, record.WorkspaceId, ct))
                throw new InvalidOperationException($"Model '{request.Model ?? record.Model ?? ProviderRegistry.DefaultModel}' is not allowed for provider '{provider}'.");
            record.Provider = provider;
        }

        if (request.Model is not null)
        {
            if (!await _providerService.IsModelAllowedAsync(record.Provider, request.Model, record.WorkspaceId, ct))
                throw new InvalidOperationException($"Model '{request.Model}' is not allowed for provider '{record.Provider}'.");
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
        return record;
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
        await _agentChannelBinder.BindBySlugsAsync(agentId, init.ChannelSlugs, ct);

        if (init.ToolNames is { Count: > 0 })
        {
            var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
            var servers = await _integrationDefinitionService.ListAsync(userId, agent?.WorkspaceId, ct);
            var names = servers.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var toolName in init.ToolNames)
            {
                var parsed = ToolKey.Parse(toolName);
                var integrationName = names.Contains(toolName) ? toolName : parsed.SkillName;
                if (names.Contains(integrationName))
                    await _integrationDefinitionService.AssignToAgentAsync(agentId, integrationName, userId, ct);
            }
        }

        if (init.ToolPermissions is { Count: > 0 })
        {
            foreach (var permission in init.ToolPermissions)
            {
                var key = AgentToolPermissionResolver.NormalizeDashboardKey(permission.Tool);
                await _agentToolPermissionRepository.UpsertAsync(agentId, key.SkillName, key.ToolName, permission.Mode, ct);
            }
        }

        // Bootstrap message
        if (!string.IsNullOrWhiteSpace(init.BootstrapMessage))
        {
            await _agentLogService.SendMessageAsync(agentId, init.BootstrapMessage, userId, ct);
        }
    }

    private async Task<bool> RequiresConfiguredProviderKeyAsync(string provider, Guid? workspaceId, CancellationToken ct)
    {
        var key = await _providerService.GetApiKeyForDispatchAsync(provider, workspaceId, ct);
        return key is null;
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
}

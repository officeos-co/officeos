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
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;

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
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser)
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
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
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

        if (!await HasConfiguredProviderAuthAsync(request.Provider, workspaceId, ct))
        {
            throw new InvalidOperationException(
                $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfigJson)
            && !await _providerService.IsModelAllowedAsync(request.Provider, request.Model, workspaceId, ct))
            throw new InvalidOperationException($"Model '{request.Model ?? ProviderRegistry.DefaultModel}' is not allowed for provider '{request.Provider}'.");

        var config = string.IsNullOrWhiteSpace(request.ConfigJson)
            ? _agentDefinitionParser.CreateDefaultConfig(
                request.Name,
                string.IsNullOrWhiteSpace(request.Model) ? ProviderRegistry.DefaultModel : request.Model,
                request.Prompt,
                null)
            : _agentDefinitionParser.Parse(request.ConfigJson);

        if (!await _providerService.IsModelAllowedAsync(request.Provider, config.Model, workspaceId, ct))
            throw new InvalidOperationException($"Model '{config.Model}' is not allowed for provider '{request.Provider}'.");

        var record = AgentRecord.Create(config.Name, request.Provider, config.Model, ownerId, config.System, workspaceId);

        await _agentRepository.AddAsync(record, ct);

        var definition = _agentDefinitionParser.CreateRecord(record.Id, 1, config, record.Provider, ownerId);
        await _agentDefinitionRepository.AddAsync(definition, ct);
        record.ActiveDefinitionId = definition.Id;
        record.Name = definition.Name;
        record.Model = definition.Model;
        record.Prompt = definition.SystemPrompt;
        await _agentRepository.UpdateAsync(record, ct);

        // Seed default personality files — domain owns the content and validation.
        var defaults = AgentPersonalityRecord.CreateDefaults(record.Id, record.Name);
        foreach (var personality in defaults)
            await _agentPersonalityRepository.UpsertAsync(record.Id, personality.FileName, personality.Content, ct);

        // If the user supplied a system prompt, merge it into BOOTSTRAP.md
        // while preserving the domain-owned default bootstrap guidance.
        if (!string.IsNullOrWhiteSpace(record.Prompt))
        {
            await _agentPersonalityRepository.UpsertAsync(
                record.Id, "BOOTSTRAP.md", AgentPersonalityRecord.CreateBootstrapContent(record.Prompt), ct);
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

        var provider = record.Provider;
        if (!string.IsNullOrWhiteSpace(request.Provider))
        {
            provider = request.Provider.Trim().ToLowerInvariant();
            if (!await HasConfiguredProviderAuthAsync(provider, record.WorkspaceId, ct))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider}' is not configured. Set its API key on the Providers page first.");
            }
            if (!await _providerService.IsModelAllowedAsync(provider, request.Model ?? record.Model, record.WorkspaceId, ct))
                throw new InvalidOperationException($"Model '{request.Model ?? record.Model ?? ProviderRegistry.DefaultModel}' is not allowed for provider '{provider}'.");
        }

        AgentDefinitionConfig config;
        if (!string.IsNullOrWhiteSpace(request.ConfigJson))
        {
            config = _agentDefinitionParser.Parse(request.ConfigJson);
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(request.Name) ? record.Name : request.Name.Trim();
            var model = request.Model ?? record.Model ?? ProviderRegistry.DefaultModel;
            var prompt = request.Prompt ?? record.Prompt;
            var activeDefinition = await _agentDefinitionRepository.GetByAsync(
                new AgentDefinitionFilter { AgentId = id, ActiveOnly = true },
                ct);
            var activeConfig = activeDefinition is null ? null : _agentDefinitionParser.Parse(activeDefinition.ConfigJson);
            config = new AgentDefinitionConfig(
                name,
                activeConfig?.Description,
                model,
                prompt,
                activeConfig?.McpServers ?? [],
                activeConfig?.Tools ?? [],
                activeConfig?.Resources,
                activeConfig?.Routines,
                activeConfig?.Metadata);
        }

        if (!await _providerService.IsModelAllowedAsync(provider, config.Model, record.WorkspaceId, ct))
            throw new InvalidOperationException($"Model '{config.Model}' is not allowed for provider '{provider}'.");

        record.Provider = provider;
        record.Name = config.Name;
        record.Prompt = config.System;
        record.ValidateAndSetModel(config.Model);

        var nextVersion = await _agentDefinitionRepository.GetNextVersionAsync(record.Id, ct);
        var definition = _agentDefinitionParser.CreateRecord(record.Id, nextVersion, config, record.Provider, record.OwnerId);

        record.ActiveDefinitionId = definition.Id;
        await _agentDefinitionRepository.AddAsync(definition, ct);
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

    public async Task<AgentLogRecord> SendMessageAsync(
        Guid agentId,
        string content,
        Guid userId,
        CancellationToken ct = default,
        string? runPurpose = null,
        Guid? definitionId = null)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent is null)
            throw new InvalidOperationException($"Agent {agentId} not found");

        var correlationId = Guid.NewGuid().ToString("N");
        var record = await _agentLogService.AppendAsync(AgentLogRecord.MessageIn(agentId, content, correlationId));
        await _publisher.Publish(new MessageReceivedEvent(
            agentId,
            content,
            correlationId,
            AgentRunPurposeKinds.Normalize(runPurpose),
            definitionId), ct);
        return record;
    }

    public async Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default)
    {
        await _agentChannelBinder.BindByConnectionIdsAsync(agentId, init.ChannelConnectionIds, ct);

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

        var activeDefinition = await _agentDefinitionRepository.GetByAsync(
            new AgentDefinitionFilter { AgentId = agentId, ActiveOnly = true },
            ct);
        if (activeDefinition is not null)
        {
            var config = _agentDefinitionParser.Parse(activeDefinition.ConfigJson);
            foreach (var server in config.McpServers)
            {
                if (server.Type == "url" && !string.IsNullOrWhiteSpace(server.Url))
                {
                    var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
                    var existing = await _integrationDefinitionService.GetAsync(userId, server.Name, agent?.WorkspaceId, ct);
                    if (existing is null && agent?.WorkspaceId is { } definitionWorkspaceId)
                    {
                        await _integrationDefinitionService.RegisterAsync(userId, definitionWorkspaceId, new IntegrationDefinitionRecord
                        {
                            Name = server.Name,
                            Title = server.Name,
                            TransportType = IntegrationTransportType.StreamableHttp,
                            Url = server.Url,
                        }, ct);
                    }
                }

                await _integrationDefinitionService.AssignToAgentAsync(agentId, server.Name, userId, ct);
            }
        }

        // Bootstrap message
        if (!string.IsNullOrWhiteSpace(init.BootstrapMessage))
        {
            await SendMessageAsync(
                agentId,
                init.BootstrapMessage,
                userId,
                ct,
                AgentRunPurposeKinds.Bootstrap,
                activeDefinition?.Id);
        }
    }

    private async Task<bool> HasConfiguredProviderAuthAsync(string provider, Guid? workspaceId, CancellationToken ct)
    {
        var auth = await _providerService.GetAuthForDispatchAsync(provider, workspaceId, ct);
        return auth is not null;
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

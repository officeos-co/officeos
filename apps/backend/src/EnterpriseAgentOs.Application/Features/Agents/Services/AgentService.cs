using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDeployer _agentDeployer;
    private readonly IProviderService _providerService;
    private readonly ILogger<AgentService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IAgentPersonalityRepository _agentPersonalityRepository;
    private readonly IPublisher _publisher;
    private readonly IChannelRepository _channelRepository;
    private readonly IAgentLogService _agentLogService;

    private static readonly TimeSpan AgentCacheTtl = TimeSpan.FromSeconds(30);
    private const string AgentListCacheKey = "agents:list";
    private static string AgentCacheKey(Guid id) => $"agents:{id}";

    public AgentService(
        IAgentRepository repository,
        IAgentDeployer deployer,
        IProviderService providerService,
        ILogger<AgentService> logger,
        IMemoryCache cache,
        IAgentPersonalityRepository personalityRepo,
        IPublisher publisher,
        IChannelRepository channelRepository,
        IAgentLogService agentLogService)
    {
        _agentRepository = repository;
        _agentDeployer = deployer;
        _providerService = providerService;
        _logger = logger;
        _memoryCache = cache;
        _agentPersonalityRepository = personalityRepo;
        _publisher = publisher;
        _channelRepository = channelRepository;
        _agentLogService = agentLogService;
    }

    public async Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(AgentListCacheKey, out IReadOnlyList<AgentDto>? cached) && cached is not null)
            return cached;

        var records = await _agentRepository.ListAsync(ct);
        _logger.LogDebug("Listing {Count} agents, refreshing pod status", records.Count);
        await Task.WhenAll(records
            .Where(r => !string.IsNullOrEmpty(r.PodName))
            .Select(r => RefreshStatusAsync(r, ct)));
        var result = records.Select(ToDto).ToList();

        _memoryCache.Set(AgentListCacheKey, (IReadOnlyList<AgentDto>)result,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = AgentCacheTtl });
        return result;
    }

    public async Task<AgentDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var key = AgentCacheKey(id);
        if (_memoryCache.TryGetValue(key, out AgentDto? cached) && cached is not null)
            return cached;

        var record = await _agentRepository.GetAsync(id, ct);
        if (record is null)
        {
            _logger.LogDebug("Agent {AgentId} not found", id);
            return null;
        }
        await RefreshStatusAsync(record, ct);
        var dto = ToDto(record);

        _memoryCache.Set(key, dto,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = AgentCacheTtl });
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
        var record = await _agentRepository.GetAsync(id, ct);
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
        var record = await _agentRepository.GetAsync(id, ct);
        if (record is null)
        {
            _logger.LogWarning("Delete failed: agent {AgentId} not found", id);
            return false;
        }

        _logger.LogInformation("Deleting agent {AgentId} ({AgentName})", id, record.Name);

        var deleted = await _agentRepository.SoftDeleteAsync(id, ct);

        if (deleted)
            await _publisher.Publish(new AgentDeletedEvent(id, record.PodName, record.HasPod, record.OwnerId), ct);

        return deleted;
    }

    public async Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default)
    {
        // Channel bindings
        if (init.ChannelSlugs is { Count: > 0 })
        {
            var connections = await _channelRepository.ListConnectionsAsync(ct);
            foreach (var slug in init.ChannelSlugs)
            {
                var match = connections.FirstOrDefault(c =>
                    string.Equals(c.ChannelType.ToStorageString(), slug, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue;
                try
                {
                    await _channelRepository.CreateBindingAsync(new AgentChannelBindingRecord
                    {
                        AgentId = agentId,
                        ChannelConnectionId = match.Id,
                    }, ct);
                }
                catch (DbUpdateException)
                {
                    // already bound — skip
                }
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
        if (!record.HasPod) return;
        try
        {
            var live = await _agentDeployer.GetStatusAsync(record.PodName, ct);
            var liveStatus = live.ToAgentStatus();
            if (liveStatus != record.Status)
            {
                await _agentRepository.UpdateStatusAsync(record.Id, liveStatus, ct);
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

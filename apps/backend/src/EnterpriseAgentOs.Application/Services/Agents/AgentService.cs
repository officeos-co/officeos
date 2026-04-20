namespace EnterpriseAgentOs.Application.Services.Agents;

public sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDeployer _agentDeployer;
    private readonly IProviderService _providerService;
    private readonly IPostHogService _postHogService;
    private readonly ILogger<AgentService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IAgentPersonalityRepository _agentPersonalityRepository;

    private static readonly TimeSpan AgentCacheTtl = TimeSpan.FromSeconds(30);
    private const string AgentListCacheKey = "agents:list";
    private static string AgentCacheKey(Guid id) => $"agents:{id}";

    public AgentService(
        IAgentRepository repository,
        IAgentDeployer deployer,
        IProviderService providerService,
        IPostHogService analytics,
        ILogger<AgentService> logger,
        IMemoryCache cache,
        IAgentPersonalityRepository personalityRepo)
    {
        _agentRepository = repository;
        _agentDeployer = deployer;
        _providerService = providerService;
        _postHogService = analytics;
        _logger = logger;
        _memoryCache = cache;
        _agentPersonalityRepository = personalityRepo;
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
        var isKeyless = Domain.Services.KnownProviders.IsKeyless(request.Provider);
        if (!isKeyless)
        {
            var apiKey = await _providerService.GetDecryptedKeyAsync(request.Provider, ct);
            if (apiKey is null)
            {
                throw new InvalidOperationException(
                    $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
            }
        }

        var record = new AgentRecord
        {
            Name = request.Name.Trim(),
            Provider = request.Provider.Trim().ToLowerInvariant(),
            Status = "pending",
            OwnerId = ownerId,
            Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? null : request.Prompt,
        };

        record.ValidateAndSetModel(request.Model);

        await _agentRepository.AddAsync(record, ct);

        // Seed default personality files — domain owns the content and validation.
        var defaults = AgentPersonalityRecord.CreateDefaults(record.Id, record.Name);
        foreach (var personality in defaults)
            await _agentPersonalityRepository.UpsertAsync(record.Id, personality.FileName, personality.Content, ct);

        _logger.LogInformation("Agent {AgentId} record created: {AgentName} ({Provider}/{Model})",
            record.Id, record.Name, record.Provider, record.Model);

        try
        {
            var deployment = await _agentDeployer.DeployAsync(record.Id, ct);

            record.MarkDeployed(deployment.PodName, deployment.ServiceUrl);
            await _agentRepository.UpdateAsync(record, ct);
            _logger.LogInformation("Agent {AgentId} deployed as pod {PodName}", record.Id, record.PodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy agent {AgentId}", record.Id);
            record.MarkFailed();
            await _agentRepository.UpdateAsync(record, ct);
        }

        _memoryCache.Remove(AgentListCacheKey);
        _memoryCache.Remove(AgentCacheKey(record.Id));

        if (ownerId is not null)
        {
            await _postHogService.CaptureAsync(
                ownerId.Value.ToString(),
                "agent_created",
                new Dictionary<string, object?>
                {
                    ["agent_id"] = record.Id,
                    ["provider"] = record.Provider,
                    ["model"] = record.Model,
                },
                ct);
        }

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
            if (!Domain.Services.KnownProviders.IsKeyless(provider))
            {
                var key = await _providerService.GetDecryptedKeyAsync(provider, ct);
                if (key is null)
                {
                    throw new InvalidOperationException(
                        $"Provider '{provider}' is not configured.");
                }
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
        _memoryCache.Remove(AgentListCacheKey);
        _memoryCache.Remove(AgentCacheKey(id));
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

        if (record.HasPod)
        {
            _logger.LogInformation("Removing pod {PodName} for agent {AgentId}", record.PodName, id);
            await _agentDeployer.RemoveAsync(record.PodName!, ct);
        }

        var deleted = await _agentRepository.SoftDeleteAsync(id, ct);
        _memoryCache.Remove(AgentListCacheKey);
        _memoryCache.Remove(AgentCacheKey(id));

        if (deleted && record.OwnerId is not null)
        {
            await _postHogService.CaptureAsync(
                record.OwnerId.Value.ToString(),
                "agent_deleted",
                new Dictionary<string, object?> { ["agent_id"] = id },
                ct);
        }

        return deleted;
    }

    private async Task RefreshStatusAsync(AgentRecord record, CancellationToken ct)
    {
        if (!record.HasPod) return;
        try
        {
            var live = await _agentDeployer.GetStatusAsync(record.PodName, ct);
            if (live != record.Status)
            {
                await _agentRepository.UpdateStatusAsync(record.Id, live, ct);
                record.Status = live;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh status for agent {AgentId}", record.Id);
        }
    }

    private static AgentDto ToDto(AgentRecord record) =>
        new(record.Id, record.Name, record.Provider, record.Model, record.Prompt, record.Status,
            record.PodName, record.ServiceUrl, record.CreatedAt);
}

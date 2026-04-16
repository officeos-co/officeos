namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _repository;
    private readonly IAgentDeployer _deployer;
    private readonly EnterpriseAgentOs.Api.Entities.Providers.IProviderService _providerService;
    private readonly EnterpriseAgentOs.Api.Entities.PostHog.IPostHogService _analytics;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentRepository repository,
        IAgentDeployer deployer,
        EnterpriseAgentOs.Api.Entities.Providers.IProviderService providerService,
        EnterpriseAgentOs.Api.Entities.PostHog.IPostHogService analytics,
        ILogger<AgentService> logger)
    {
        _repository = repository;
        _deployer = deployer;
        _providerService = providerService;
        _analytics = analytics;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        _logger.LogDebug("Listing {Count} agents, refreshing pod status", records.Count);
        await Task.WhenAll(records
            .Where(r => !string.IsNullOrEmpty(r.PodName))
            .Select(r => RefreshStatusAsync(r, ct)));
        return records.Select(ToDto).ToList();
    }

    public async Task<AgentDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null)
        {
            _logger.LogDebug("Agent {AgentId} not found", id);
            return null;
        }
        await RefreshStatusAsync(record, ct);
        return ToDto(record);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating agent {AgentName} with provider {Provider} model {Model}",
            request.Name, request.Provider, request.Model);
        // Validate provider is configured (key exists). The real key is
        // NOT passed to the deployer — the LLM proxy resolves it per-request.
        var isKeyless = IsKeylessProvider(request.Provider);
        if (!isKeyless)
        {
            var apiKey = await _providerService.GetDecryptedKeyAsync(request.Provider, ct);
            if (apiKey is null)
            {
                throw new InvalidOperationException(
                    $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
            }
        }

        var record = new EnterpriseAgentOs.Api.Database.Models.AgentRecord
        {
            Name = request.Name.Trim(),
            Provider = request.Provider.Trim().ToLowerInvariant(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            Status = "pending",
            OwnerId = ownerId,
            Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? null : request.Prompt,
        };

        if (record.Model is null)
        {
            // Default to "auto" — SmartRouter will pick the appropriate model at
            // inference time based on request complexity and agent provider family.
            record.Model = "auto";
        }
        else if (!EnterpriseAgentOs.Api.Entities.Providers.KnownModels.IsValid(record.Model))
        {
            var allowed = string.Join(", ", EnterpriseAgentOs.Api.Entities.Providers.KnownModels.SupportedModels);
            throw new InvalidOperationException(
                $"Model '{record.Model}' is not a known model. " +
                $"Allowed: {allowed}");
        }

        await _repository.AddAsync(record, ct);
        _logger.LogInformation("Agent {AgentId} record created: {AgentName} ({Provider}/{Model})",
            record.Id, record.Name, record.Provider, record.Model);

        try
        {
            // The deployer only sets ZEROCLAW_AGENT_ID. The agent derives
            // everything else (provider, model, skills, system prompt) from
            // its ID by calling back to this backend at runtime. Personality
            // .md files are embedded in the zeroclaw-core binary — the
            // backend no longer provisions any per-agent vault.
            var deployment = await _deployer.DeployAsync(record.Id, ct);

            record.PodName = deployment.PodName;
            record.ServiceUrl = deployment.ServiceUrl;
            record.Status = "running";
            await _repository.UpdateAsync(record, ct);
            _logger.LogInformation("Agent {AgentId} deployed as pod {PodName}", record.Id, record.PodName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy agent {AgentId}", record.Id);
            record.Status = "failed";
            await _repository.UpdateAsync(record, ct);
        }

        if (ownerId is not null)
        {
            await _analytics.CaptureAsync(
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
        var record = await _repository.GetAsync(id, ct);
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
            if (!IsKeylessProvider(provider))
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
            var model = request.Model.Trim();
            if (model.Length > 0 && !EnterpriseAgentOs.Api.Entities.Providers.KnownModels.IsValid(model))
            {
                throw new InvalidOperationException(
                    $"Model '{model}' is not a known model.");
            }
            record.Model = model.Length > 0 ? model : "auto";
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            record.Name = request.Name.Trim();
        }

        if (request.Prompt is not null)
        {
            record.Prompt = request.Prompt.Length == 0 ? null : request.Prompt;
        }

        await _repository.UpdateAsync(record, ct);
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null)
        {
            _logger.LogWarning("Delete failed: agent {AgentId} not found", id);
            return false;
        }

        _logger.LogInformation("Deleting agent {AgentId} ({AgentName})", id, record.Name);

        if (!string.IsNullOrEmpty(record.PodName))
        {
            _logger.LogInformation("Removing pod {PodName} for agent {AgentId}", record.PodName, id);
            await _deployer.RemoveAsync(record.PodName, ct);
        }

        var deleted = await _repository.SoftDeleteAsync(id, ct);

        if (deleted && record.OwnerId is not null)
        {
            await _analytics.CaptureAsync(
                record.OwnerId.Value.ToString(),
                "agent_deleted",
                new Dictionary<string, object?> { ["agent_id"] = id },
                ct);
        }

        return deleted;
    }

    private async Task RefreshStatusAsync(EnterpriseAgentOs.Api.Database.Models.AgentRecord record, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(record.PodName)) return;
        try
        {
            var live = await _deployer.GetStatusAsync(record.PodName, ct);
            if (live != record.Status)
            {
                await _repository.UpdateStatusAsync(record.Id, live, ct);
                record.Status = live;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh status for agent {AgentId}", record.Id);
        }
    }

    /// <summary>
    /// Providers that do NOT require a BYOK API key in the database —
    /// either they are local (ollama) or their keys are held by the platform
    /// (anthropic, google, xai) and injected via LiteLLM.
    /// </summary>
    private static readonly HashSet<string> KeylessProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ollama", "anthropic", "google", "xai",
        };

    private static bool IsKeylessProvider(string name) =>
        KeylessProviders.Contains(name);

    private static AgentDto ToDto(EnterpriseAgentOs.Api.Database.Models.AgentRecord record) =>
        new(record.Id, record.Name, record.Provider, record.Model, record.Prompt, record.Status,
            record.PodName, record.ServiceUrl, record.CreatedAt);
}

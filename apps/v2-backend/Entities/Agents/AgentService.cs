
namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _repository;
    private readonly IAgentDeployer _deployer;
    private readonly IProviderService _providerService;
    private readonly IVaultClient _vault;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentRepository repository,
        IAgentDeployer deployer,
        IProviderService providerService,
        IVaultClient vault,
        ILogger<AgentService> logger)
    {
        _repository = repository;
        _deployer = deployer;
        _providerService = providerService;
        _vault = vault;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        await Task.WhenAll(records
            .Where(r => !string.IsNullOrEmpty(r.PodName))
            .Select(r => RefreshStatusAsync(r, ct)));
        return records.Select(ToDto).ToList();
    }

    public async Task<AgentDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null) return null;
        await RefreshStatusAsync(record, ct);
        return ToDto(record);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentRequest request, CancellationToken ct = default)
    {
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

        var record = new AgentRecord
        {
            Name = request.Name.Trim(),
            Provider = request.Provider.Trim().ToLowerInvariant(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            Status = "pending",
        };

        if (record.Model is null)
        {
            // "(provider default)" on the frontend arrives here as null.
            // zeroclaw has a compiled-in default of anthropic/claude-sonnet-4.6
            // which it will try to route through whatever PROVIDER env says —
            // so if we leave Model null and the deployer omits ZEROCLAW_MODEL,
            // the pod boots with a vendor-mismatched model and every chat 400s.
            // Substitute a concrete provider-appropriate default here so the
            // pod always gets an explicit ZEROCLAW_MODEL for its PROVIDER.
            var defaultModel = KnownModels.For(record.Provider).FirstOrDefault();
            if (defaultModel is null)
            {
                throw new InvalidOperationException(
                    $"Provider '{record.Provider}' has no known models configured. " +
                    "Add an entry to KnownModels.cs before creating agents with this provider.");
            }
            record.Model = defaultModel;
        }
        else if (!KnownModels.IsValid(record.Provider, record.Model))
        {
            var allowed = string.Join(", ", KnownModels.For(record.Provider));
            throw new InvalidOperationException(
                $"Model '{record.Model}' is not a known model for provider '{record.Provider}'. " +
                $"Allowed: {(allowed.Length == 0 ? "(none)" : allowed)}");
        }

        await _repository.AddAsync(record, ct);

        try
        {
            await _vault.CreateAgentVaultAsync(record.Id, record.Name, record.Provider, record.Model, ct);

            // The deployer only sets ZEROCLAW_AGENT_ID. The agent derives
            // everything else (provider, model, vault, skills) from its
            // ID by calling back to this backend at runtime.
            var deployment = await _deployer.DeployAsync(record.Id, ct);

            record.PodName = deployment.PodName;
            record.ServiceUrl = deployment.ServiceUrl;
            record.Status = "running";
            await _repository.UpdateAsync(record, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy agent {AgentId}", record.Id);
            record.Status = "failed";
            await _repository.UpdateAsync(record, ct);
        }

        return ToDto(record);
    }

    public async Task<AgentDto?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null) return null;

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
            if (model.Length > 0 && !KnownModels.IsValid(record.Provider, model))
            {
                throw new InvalidOperationException(
                    $"Model '{model}' is not a known model for provider '{record.Provider}'.");
            }
            record.Model = model.Length > 0 ? model : KnownModels.For(record.Provider).FirstOrDefault();
        }

        await _repository.UpdateAsync(record, ct);
        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        if (record is null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(record.PodName))
        {
            await _deployer.RemoveAsync(record.PodName, ct);
        }

        try
        {
            await _vault.DeleteAgentVaultAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete vault for agent {AgentId}", id);
        }

        return await _repository.SoftDeleteAsync(id, ct);
    }

    private async Task RefreshStatusAsync(AgentRecord record, CancellationToken ct)
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

    private static bool IsKeylessProvider(string name) =>
        name.Equals("ollama", StringComparison.OrdinalIgnoreCase);

    private static AgentDto ToDto(AgentRecord record) =>
        new(record.Id, record.Name, record.Provider, record.Model, record.Status,
            record.PodName, record.ServiceUrl, record.CreatedAt);
}

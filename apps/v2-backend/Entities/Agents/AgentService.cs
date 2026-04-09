
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
        return records.Select(ToDto).ToList();
    }

    public async Task<AgentDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetAsync(id, ct);
        return record is null ? null : ToDto(record);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentRequest request, CancellationToken ct = default)
    {
        var apiKey = await _providerService.GetDecryptedKeyAsync(request.Provider, ct);
        if (apiKey is null && !IsKeylessProvider(request.Provider))
        {
            throw new InvalidOperationException(
                $"Provider '{request.Provider}' is not configured. Set its API key on the Providers page first.");
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

            var deployment = await _deployer.DeployAsync(
                record.Id,
                record.Provider,
                apiKey ?? string.Empty,
                record.Model,
                ct);

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

    private static bool IsKeylessProvider(string name) =>
        name.Equals("ollama", StringComparison.OrdinalIgnoreCase);

    private static AgentDto ToDto(AgentRecord record) =>
        new(record.Id, record.Name, record.Provider, record.Model, record.Status,
            record.PodName, record.ServiceUrl, record.CreatedAt);
}

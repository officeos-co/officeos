namespace OffceOs.Application.Features.Agents;

internal sealed class DeclarativeAgentService : IDeclarativeAgentService
{
    private readonly IAgentDashboardService _agentDashboardService;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly AgentManifestParser _agentManifestParser;

    public DeclarativeAgentService(
        IAgentDashboardService agentDashboardService,
        IAgentRepository agentRepository,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        AgentManifestParser agentManifestParser)
    {
        _agentDashboardService = agentDashboardService;
        _agentRepository = agentRepository;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _agentManifestParser = agentManifestParser;
    }

    public Task<DeclarativeManifestValidationResult> ValidateAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        try
        {
            var manifests = _agentManifestParser.ParseMany(manifest);
            var resources = manifests
                .Select(item => $"Agent/{RequireName(item)}")
                .ToList();
            foreach (var item in manifests)
            {
                RequireProvider(item);
                _ = _agentManifestParser.ToDefinitionConfig(item);
            }

            return Task.FromResult(new DeclarativeManifestValidationResult(true, [], resources));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(new DeclarativeManifestValidationResult(false, [ex.Message], []));
        }
    }

    public async Task<DeclarativeManifestDiffResult> DiffAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var changes = new List<DeclarativeAgentChangeItem>();
        foreach (var item in _agentManifestParser.ParseMany(manifest))
            changes.Add(await BuildChangeAsync(item, workspaceId, ct));

        return new DeclarativeManifestDiffResult(changes);
    }

    public async Task<DeclarativeManifestApplyResult> ApplyAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var changes = new List<DeclarativeAgentChangeItem>();
        foreach (var item in _agentManifestParser.ParseMany(manifest))
        {
            var change = await BuildChangeAsync(item, workspaceId, ct);
            if (change.Action == "create")
            {
                var config = _agentManifestParser.ToDefinitionConfig(item);
                var agent = await _agentDashboardService.CreateAsync(
                    new CreateDashboardAgentRequest(
                        config.Name,
                        RequireProvider(item),
                        config.Model,
                        config.System,
                        _agentDefinitionParser.Serialize(config),
                        null,
                        null,
                        null,
                        null,
                        null),
                    ownerId,
                    workspaceId,
                    ct);
                changes.Add(change with { AgentId = agent.Id.ToString(), Message = "Created agent." });
                continue;
            }

            if (change.Action == "update" && change.AgentId is not null)
            {
                var config = _agentManifestParser.ToDefinitionConfig(item);
                var patched = await _agentDashboardService.PatchAsync(
                    Guid.Parse(change.AgentId),
                    ownerId,
                    workspaceId,
                    new PatchAgentRequest(RequireProvider(item), config.Model, config.Name, config.System, _agentDefinitionParser.Serialize(config)),
                    ct);
                changes.Add(change with { AgentId = patched?.Id.ToString(), Message = "Updated agent definition." });
                continue;
            }

            changes.Add(change);
        }

        return new DeclarativeManifestApplyResult(changes);
    }

    public async Task<string?> ExportAgentAsync(string name, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        var agent = await FindByNameAsync(name, workspaceId, ct);
        if (agent is null)
            return null;

        var definition = await _agentDefinitionRepository.GetByAsync(new AgentDefinitionFilter { AgentId = agent.Id, ActiveOnly = true }, ct);
        if (definition is null)
            return null;

        var config = _agentDefinitionParser.Parse(definition.ConfigJson);
        return _agentManifestParser.Serialize(_agentManifestParser.FromDefinition(agent.Provider, config));
    }

    private async Task<DeclarativeAgentChangeItem> BuildChangeAsync(AgentManifestItem manifest, Guid workspaceId, CancellationToken ct)
    {
        var name = RequireName(manifest);
        var provider = RequireProvider(manifest);
        var config = _agentManifestParser.ToDefinitionConfig(manifest);
        var agent = await FindByNameAsync(name, workspaceId, ct);
        if (agent is null)
            return new DeclarativeAgentChangeItem("Agent", name, "create", null, "Agent does not exist.");

        var definition = await _agentDefinitionRepository.GetByAsync(new AgentDefinitionFilter { AgentId = agent.Id, ActiveOnly = true }, ct);
        var desiredHash = Hash(_agentDefinitionParser.Serialize(config));
        var currentHash = definition?.ConfigHash;
        if (string.Equals(agent.Provider, provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(currentHash, desiredHash, StringComparison.OrdinalIgnoreCase))
        {
            return new DeclarativeAgentChangeItem("Agent", name, "none", agent.Id.ToString(), "Agent is unchanged.");
        }

        return new DeclarativeAgentChangeItem("Agent", name, "update", agent.Id.ToString(), "Agent differs from manifest.");
    }

    private async Task<AgentRecord?> FindByNameAsync(string name, Guid workspaceId, CancellationToken ct)
    {
        var agents = await _agentRepository.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        return agents.FirstOrDefault(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string RequireName(AgentManifestItem manifest) =>
        string.IsNullOrWhiteSpace(manifest.Metadata?.Name)
            ? throw new InvalidOperationException("Agent manifest metadata.name is required.")
            : manifest.Metadata.Name.Trim();

    private static string RequireProvider(AgentManifestItem manifest) =>
        string.IsNullOrWhiteSpace(manifest.Spec?.Provider)
            ? throw new InvalidOperationException($"Agent manifest '{RequireName(manifest)}' requires spec.provider.")
            : manifest.Spec.Provider.Trim().ToLowerInvariant();

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

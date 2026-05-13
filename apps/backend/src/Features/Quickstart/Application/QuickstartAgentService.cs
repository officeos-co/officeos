namespace OffceOs.Application.Features.Quickstart;

internal sealed class QuickstartAgentService : IQuickstartAgentService
{
    private const int MaxCatalogToolsPerIntegration = 12;

    private readonly IProviderService _providerService;
    private readonly IProviderDispatchService _providerDispatchService;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IAgentResourceRepository _agentResourceRepository;
    private readonly IAgentResourceService _agentResourceService;
    private readonly IAgentDashboardService _agentDashboardService;
    private readonly IChannelRepository _channelRepository;
    private readonly IMemoryStoreRepository _memoryStoreRepository;
    private readonly IAgentRoutineService _agentRoutineService;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly QuickstartBlueprintParser _quickstartBlueprintParser;
    private readonly SseResponseParser _sseResponseParser;

    public QuickstartAgentService(
        IProviderService providerService,
        IProviderDispatchService providerDispatchService,
        IIntegrationDefinitionService integrationDefinitionService,
        IAgentResourceRepository agentResourceRepository,
        IAgentResourceService agentResourceService,
        IAgentDashboardService agentDashboardService,
        IChannelRepository channelRepository,
        IMemoryStoreRepository memoryStoreRepository,
        IAgentRoutineService agentRoutineService,
        AgentDefinitionParser agentDefinitionParser,
        QuickstartBlueprintParser quickstartBlueprintParser,
        SseResponseParser sseResponseParser)
    {
        _providerService = providerService;
        _providerDispatchService = providerDispatchService;
        _integrationDefinitionService = integrationDefinitionService;
        _agentResourceRepository = agentResourceRepository;
        _agentResourceService = agentResourceService;
        _agentDashboardService = agentDashboardService;
        _channelRepository = channelRepository;
        _memoryStoreRepository = memoryStoreRepository;
        _agentRoutineService = agentRoutineService;
        _agentDefinitionParser = agentDefinitionParser;
        _quickstartBlueprintParser = quickstartBlueprintParser;
        _sseResponseParser = sseResponseParser;
    }

    public async Task<QuickstartAgentChatResult> ChatAsync(
        QuickstartAgentChatRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("Message is required.");
        if (!IsReasonableRequest(request.Message))
            throw new InvalidOperationException("Describe a concrete agent goal, workflow, or edit. Very short placeholders such as 'test' are not enough to generate a useful agent definition.");

        var context = await BuildContextAsync(userId, workspaceId, ct);
        var provider = string.IsNullOrWhiteSpace(request.Model)
            ? ResolveProvider(request.Provider, context.Models)
            : ResolveProviderForModel(request.Provider, request.Model.Trim(), context.Models);
        var model = await ResolveModelAsync(provider, request.Model, workspaceId, context.Models, ct);
        var currentFiles = BuildCurrentFiles(request, model);

        var requestBody = JsonSerializer.SerializeToElement(new
        {
            model,
            messages = BuildMessages(request, currentFiles, context, model),
            stream = true,
        });

        var dispatch = await _providerDispatchService.DispatchAsync(provider, workspaceId, model, requestBody, ct);
        if (dispatch.IsFailure)
            throw new InvalidOperationException(dispatch.Error.Message);

        var parsed = await _sseResponseParser.ParseAsync(dispatch.Value.Response, ct);
        var output = ParseModelOutput(parsed.Content ?? string.Empty);
        var files = NormalizeOutputFiles(output, currentFiles);
        var config = GetPrimaryAgentConfig(files);
        var normalizedYaml = _agentDefinitionParser.SerializeYaml(config);
        var configJson = _agentDefinitionParser.Serialize(config);

        return new QuickstartAgentChatResult(
            output.Message,
            normalizedYaml,
            configJson,
            InferProvider(provider, config.Model),
            config.Model,
            files);
    }

    public async Task<QuickstartBlueprintApplyResult> ApplyAsync(
        QuickstartBlueprintApplyRequest request,
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default)
    {
        var parsed = _quickstartBlueprintParser.Parse(request.Files);
        var models = await ListConfiguredModelsAsync(workspaceId, ct);
        var createdResources = new Dictionary<string, QuickstartCreatedResourceResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in parsed.Resources)
        {
            if (resource.Type == AgentResourceKinds.Browser)
            {
                var browser = await _agentResourceService.CreateBrowserResourceAsync(
                    userId,
                    workspaceId,
                    resource.DisplayName,
                    ct);
                createdResources[resource.Key] = new QuickstartCreatedResourceResult(resource.Key, resource.Type, browser.Id);
            }
            else if (resource.Type == AgentResourceKinds.MemoryStore)
            {
                var memoryStore = await _memoryStoreRepository.CreateAsync(
                    MemoryStoreRecord.Create(userId, workspaceId, resource.DisplayName),
                    ct);
                createdResources[resource.Key] = new QuickstartCreatedResourceResult(resource.Key, resource.Type, memoryStore.Id);
            }
        }

        var createdAgents = new List<QuickstartCreatedAgentResult>();
        foreach (var parsedAgent in parsed.Agents)
        {
            var config = _quickstartBlueprintParser.ResolveAgentConfig(parsedAgent.Agent, createdResources);
            var model = string.IsNullOrWhiteSpace(request.Model) ? config.Model : request.Model.Trim();
            var provider = ResolveProviderForModel(request.Provider, model, models);
            if (!await _providerService.IsModelAllowedAsync(provider, model, workspaceId, ct))
                throw new InvalidOperationException($"Model '{model}' is not allowed for provider '{provider}'.");
            config = config with { Model = model };

            var agent = await _agentDashboardService.CreateAsync(
                new CreateDashboardAgentRequest(
                    config.Name,
                    provider,
                    model,
                    config.System,
                    _agentDefinitionParser.Serialize(config),
                    null,
                    null,
                    null,
                    null,
                    null),
                userId,
                workspaceId,
                ct);

            createdAgents.Add(new QuickstartCreatedAgentResult(agent.Id, agent.Name, parsedAgent.FilePath));
        }

        return new QuickstartBlueprintApplyResult(createdAgents);
    }

    private static bool IsReasonableRequest(string message)
    {
        var normalized = Regex.Replace(message.Trim(), "\\s+", " ");
        if (normalized.Length < 12)
            return false;

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 3;
    }

    private async Task<QuickstartBuilderContext> BuildContextAsync(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var providers = await _providerService.ListForWorkspaceAsync(workspaceId, ct);
        return new QuickstartBuilderContext(
            ToModelContext(providers),
            await _integrationDefinitionService.ListAsync(userId, workspaceId, ct),
            await _agentResourceRepository.ListBrowserResourcesAsync(null, workspaceId, ct),
            await _channelRepository.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspaceId }, ct),
            await _memoryStoreRepository.ListAsync(null, workspaceId, ct),
            await _agentRoutineService.ListForOwnerAsync(userId, workspaceId, ct));
    }

    private async Task<IReadOnlyList<QuickstartModelContext>> ListConfiguredModelsAsync(Guid workspaceId, CancellationToken ct)
        => ToModelContext(await _providerService.ListForWorkspaceAsync(workspaceId, ct));

    private static IReadOnlyList<QuickstartModelContext> ToModelContext(IReadOnlyList<ProviderResult> providers)
        => providers
            .Where(provider => provider.Configured && provider.Models.Count > 0)
            .SelectMany(provider => provider.Models.Select(model => new QuickstartModelContext(provider.Name, model.Id, model.DisplayName, model.CostWeight)))
            .ToList();

    private static string ResolveProvider(string? provider, IReadOnlyList<QuickstartModelContext> models)
    {
        if (!string.IsNullOrWhiteSpace(provider))
            return provider.Trim().ToLowerInvariant();

        return models.FirstOrDefault(candidate => candidate.Provider == ProviderRegistry.OpenAiCodexProviderSlug)?.Provider
            ?? models.FirstOrDefault()?.Provider
            ?? throw new InvalidOperationException("No configured LLM provider is available for quickstart generation.");
    }

    private async Task<string> ResolveModelAsync(
        string provider,
        string? model,
        Guid workspaceId,
        IReadOnlyList<QuickstartModelContext> models,
        CancellationToken ct)
    {
        var defaultModel = string.IsNullOrWhiteSpace(model)
            ? models.FirstOrDefault(candidate => candidate.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))?.Id ?? ProviderRegistry.DefaultModel
            : model.Trim();

        return await _providerService.IsModelAllowedAsync(provider, defaultModel, workspaceId, ct)
            ? defaultModel
            : throw new InvalidOperationException($"Model '{defaultModel}' is not allowed for provider '{provider}'.");
    }

    private static string ResolveProviderForModel(
        string? provider,
        string model,
        IReadOnlyList<QuickstartModelContext> models)
    {
        if (!string.IsNullOrWhiteSpace(provider))
            return provider.Trim().ToLowerInvariant();

        if (model.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase))
        {
            return models.Any(candidate => candidate.Provider.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
                ? "anthropic"
                : throw new InvalidOperationException("Auto model routing requires a configured Anthropic provider.");
        }

        var candidates = models
            .Where(candidate => candidate.Id.Equals(model, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
            throw new InvalidOperationException($"Model '{model}' is not configured for this workspace.");

        var registryProvider = ProviderRegistry.GetByModel(model)?.Slug;
        return candidates.FirstOrDefault(candidate =>
                registryProvider is not null
                && candidate.Provider.Equals(registryProvider, StringComparison.OrdinalIgnoreCase))
            ?.Provider
            ?? candidates[0].Provider;
    }

    private static List<object> BuildMessages(
        QuickstartAgentChatRequest request,
        IReadOnlyList<QuickstartFileResult> currentFiles,
        QuickstartBuilderContext context,
        string model)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = $"""
                You are the EnterpriseAgentOs quickstart builder. Generate and edit declarative YAML quickstart files.

                Return only a JSON object with:
                - "message": short assistant reply for the chat
                - "files": array of complete YAML files, each with "path" and "content"

                Use this file layout by default:
                - workspace.yaml declares created resources and agent file paths
                - agents/<agent-key>.yaml declares one agent
                Single-agent creation is still the default. Create multiple agent files only when the user explicitly asks for multiple agents.

                Workspace YAML schema:
                kind: workspace
                resources:
                  browsers:
                    - key: stable_reference_name
                      display_name: user-facing browser name
                  memory_stores:
                    - key: stable_reference_name
                      display_name: user-facing memory store name
                agents:
                  - key: stable_agent_key
                    file: agents/stable-agent-key.yaml

                Agent YAML schema:
                kind: agent
                key: stable_agent_key
                name: string
                description: string
                model: string
                system: multiline string
                mcp_servers:
                  - name: integration slug
                    type: registered | url
                    url: required only for type url
                tools:
                  - type: agent_toolset_20260401
                  - type: browser_toolset
                  - type: mcp_toolset
                    mcp_server_name: integration slug
                    default_config:
                      permission_policy:
                        type: always_allow | always_deny | allow_list | deny_list
                        tools: required for allow_list and deny_list
                resources:
                  - type: browser | memory_store | channel
                    ref: workspace resource key for browser or memory_store created in workspace.yaml
                    resource_id: configured resource UUID
                    access_mode: read_write | read_only
                    instructions: optional resource-specific guidance
                routines:
                  - name: string
                    prompt: routine instruction to send to the agent
                    schedule_triggers:
                      - name: string
                        expression: cron expression
                    api_triggers:
                      - name: string
                    github_triggers:
                      - name: string
                        owner: repository owner
                        repo: repository name
                        events: [issues, pull_request]
                        secret: webhook secret
                metadata: object

                Use model "{model}" unless the user explicitly asks for another valid model.
                For now, only create browser and memory_store resources in workspace.yaml. Do not create MCP servers or channels. Existing MCP servers may be referenced by name. Existing channels may only be attached by resource_id if already configured.
                If the request is too vague, ask one concise clarifying question in "message" and keep the YAML close to the current version.
                Prefer type "registered" for configured MCP servers. Include browser_toolset only when a browser resource is attached or browser automation is required. Include agent_toolset_20260401 for normal filesystem, shell, HTTP, memory, and orchestration tools.
                Full workspace context:
                {BuildWorkspaceContext(context)}
                """
            },
            new
            {
                role = "user",
                content = $"Current files:\n{FormatFilesForPrompt(currentFiles)}"
            },
        };

        foreach (var message in request.Messages ?? [])
        {
            var role = message.Role.Equals("agent", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            if (!string.IsNullOrWhiteSpace(message.Content))
                messages.Add(new { role, content = message.Content.Trim() });
        }

        messages.Add(new { role = "user", content = request.Message.Trim() });
        return messages;
    }

    private IReadOnlyList<QuickstartFileResult> BuildCurrentFiles(QuickstartAgentChatRequest request, string model)
    {
        if (request.CurrentFiles is { Count: > 0 })
        {
            return request.CurrentFiles
                .Where(file => !string.IsNullOrWhiteSpace(file.Path) && !string.IsNullOrWhiteSpace(file.Content))
                .Select(file => new QuickstartFileResult(file.Path.Trim(), file.Content.Trim()))
                .ToList();
        }

        var config = string.IsNullOrWhiteSpace(request.CurrentYaml)
            ? _agentDefinitionParser.CreateDefaultConfig("Operations assistant", model, null, null)
            : _agentDefinitionParser.Parse(request.CurrentYaml);

        return
        [
            new QuickstartFileResult("agents/agent.yaml", _agentDefinitionParser.SerializeYaml(config)),
        ];
    }

    private IReadOnlyList<QuickstartFileResult> NormalizeOutputFiles(
        QuickstartModelResult output,
        IReadOnlyList<QuickstartFileResult> currentFiles)
    {
        if (output.Files is { Count: > 0 })
        {
            return output.Files
                .Where(file => !string.IsNullOrWhiteSpace(file.Path) && !string.IsNullOrWhiteSpace(file.Content))
                .Select(file => new QuickstartFileResult(file.Path.Trim(), file.Content.Trim()))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(output.ConfigYaml))
            return [new QuickstartFileResult(FindPrimaryAgentPath(currentFiles), output.ConfigYaml.Trim())];

        return currentFiles;
    }

    private AgentDefinitionConfig GetPrimaryAgentConfig(IReadOnlyList<QuickstartFileResult> files)
    {
        var parsed = _quickstartBlueprintParser.Parse(files.Select(file => new QuickstartFileRequest(file.Path, file.Content)).ToList());
        var agent = parsed.Agents.FirstOrDefault()
            ?? throw new InvalidOperationException("Quickstart output must include at least one agent YAML file.");
        var directResources = (agent.Agent.Resources ?? [])
            .Where(resource => resource.ResourceId is { } resourceId && resourceId != Guid.Empty)
            .Select(resource => new AgentResourceAttachmentConfig(
                resource.Type,
                resource.ResourceId!.Value,
                resource.AccessMode,
                resource.Instructions))
            .ToList();

        return _agentDefinitionParser.Parse(_agentDefinitionParser.Serialize(new AgentDefinitionConfig(
            agent.Agent.Name,
            agent.Agent.Description,
            agent.Agent.Model,
            agent.Agent.System,
            agent.Agent.McpServers ?? [],
            agent.Agent.Tools ?? [],
            directResources,
            agent.Agent.Routines,
            agent.Agent.Metadata)));
    }

    private static string FindPrimaryAgentPath(IReadOnlyList<QuickstartFileResult> files)
        => files.FirstOrDefault(file => file.Path.StartsWith("agents/", StringComparison.OrdinalIgnoreCase))?.Path
            ?? files.FirstOrDefault(file => !file.Path.Equals("workspace.yaml", StringComparison.OrdinalIgnoreCase)
                && !file.Path.Equals("workspace.yml", StringComparison.OrdinalIgnoreCase))?.Path
            ?? "agents/agent.yaml";

    private static string FormatFilesForPrompt(IReadOnlyList<QuickstartFileResult> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.AppendLine($"--- {file.Path}");
            builder.AppendLine("```yaml");
            builder.AppendLine(file.Content);
            builder.AppendLine("```");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildWorkspaceContext(QuickstartBuilderContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Available models:");
        if (context.Models.Count == 0)
            builder.AppendLine("- none configured");
        foreach (var model in context.Models.OrderBy(model => model.Provider).ThenBy(model => model.Id))
            builder.AppendLine($"- provider: {model.Provider}, model: {model.Id}, display: {model.DisplayName}, cost_weight: {model.CostWeight}");

        builder.AppendLine("Configured MCP servers:");
        builder.AppendLine(BuildCatalogContext(context.McpServers));

        builder.AppendLine("Configured browsers:");
        if (context.Browsers.Count == 0)
            builder.AppendLine("- none");
        foreach (var browser in context.Browsers.OrderBy(browser => browser.DisplayName))
            builder.AppendLine($"- id: {browser.Id}, display_name: {browser.DisplayName}, current_agent_id: {browser.CurrentAgentId}");

        builder.AppendLine("Configured channels:");
        if (context.Channels.Count == 0)
            builder.AppendLine("- none");
        foreach (var channel in context.Channels.OrderBy(channel => channel.DisplayName))
            builder.AppendLine($"- id: {channel.Id}, type: {channel.ChannelType.ToStorageString()}, display_name: {channel.DisplayName}, enabled: {channel.Enabled}");

        builder.AppendLine("Configured memory stores:");
        if (context.MemoryStores.Count == 0)
            builder.AppendLine("- none");
        foreach (var memoryStore in context.MemoryStores.OrderBy(memoryStore => memoryStore.DisplayName))
            builder.AppendLine($"- id: {memoryStore.Id}, display_name: {memoryStore.DisplayName}");

        builder.AppendLine("Existing routines:");
        if (context.Routines.Count == 0)
            builder.AppendLine("- none");
        foreach (var routine in context.Routines.OrderBy(routine => routine.AgentName).ThenBy(routine => routine.Routine.Name))
            builder.AppendLine($"- agent: {routine.AgentName}, name: {routine.Routine.Name}, enabled: {routine.Routine.Enabled}, triggers: {string.Join(", ", routine.Routine.Triggers.Select(trigger => trigger.Kind))}");

        return builder.ToString().TrimEnd();
    }

    private static string BuildCatalogContext(IReadOnlyList<IntegrationDefinitionRecord> catalog)
    {
        if (catalog.Count == 0)
            return "- No MCP integrations are available in this workspace.";

        var builder = new StringBuilder();
        foreach (var integration in catalog.OrderBy(item => item.Name))
        {
            var status = integration.CredentialConfigured || integration.OauthConfigured ? "configured" : "not_configured";
            builder.AppendLine($"- {integration.Name} ({status}): {integration.Title}. {integration.Description}");
            foreach (var tool in integration.Tools.Take(MaxCatalogToolsPerIntegration))
                builder.AppendLine($"  - {tool.Name}: {tool.Description}");
            if (integration.Tools.Count > MaxCatalogToolsPerIntegration)
                builder.AppendLine($"  - ... {integration.Tools.Count - MaxCatalogToolsPerIntegration} more tools");
        }

        return builder.ToString().TrimEnd();
    }

    private static QuickstartModelResult ParseModelOutput(string content)
    {
        try
        {
            var json = ExtractJsonObject(content);
            var parsed = JsonSerializer.Deserialize<QuickstartModelResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            if (parsed is not null
                && (!string.IsNullOrWhiteSpace(parsed.ConfigYaml) || parsed.Files is { Count: > 0 }))
                return parsed;
        }
        catch (JsonException)
        {
        }

        var yaml = ExtractYaml(content);
        return new QuickstartModelResult("I updated the agent definition.", yaml, null);
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        var fenced = Regex.Match(trimmed, "```(?:json)?\\s*(\\{[\\s\\S]*?\\})\\s*```", RegexOptions.IgnoreCase);
        if (fenced.Success)
            return fenced.Groups[1].Value;

        var start = trimmed.IndexOf('{', StringComparison.Ordinal);
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        return trimmed;
    }

    private static string ExtractYaml(string content)
    {
        var fenced = Regex.Match(content, "```(?:yaml|yml)?\\s*([\\s\\S]*?)\\s*```", RegexOptions.IgnoreCase);
        if (fenced.Success)
            return fenced.Groups[1].Value;

        return content;
    }

    private static string InferProvider(string fallbackProvider, string model)
        => ProviderRegistry.GetByModel(model)?.Slug ?? fallbackProvider;

    private sealed record QuickstartModelResult(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("yaml")] string? ConfigYaml,
        [property: JsonPropertyName("files")] IReadOnlyList<QuickstartFileResult>? Files);

    private sealed record QuickstartBuilderContext(
        IReadOnlyList<QuickstartModelContext> Models,
        IReadOnlyList<IntegrationDefinitionRecord> McpServers,
        IReadOnlyList<BrowserResourceRecord> Browsers,
        IReadOnlyList<ChannelConnectionRecord> Channels,
        IReadOnlyList<MemoryStoreRecord> MemoryStores,
        IReadOnlyList<AgentRoutineWithAgentRecord> Routines);

    private sealed record QuickstartModelContext(
        string Provider,
        string Id,
        string DisplayName,
        int CostWeight);
}

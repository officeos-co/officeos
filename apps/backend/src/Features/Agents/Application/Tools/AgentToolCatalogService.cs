namespace OffceOs.Application.Features.Agents;

internal sealed class AgentToolCatalogService : IAgentToolCatalogService
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly IAgentCronJobRepository _agentCronJobRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly AgentTaskStore _agentTaskStore;
    private readonly IBrowserToolContextFactory _browserToolContextFactory;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;

    public AgentToolCatalogService(
        IAgentMemoryService memoryService,
        IAgentCronJobRepository cronJobRepository,
        IAgentRunRepository agentRunRepository,
        AgentTaskStore taskStore,
        IBrowserToolContextFactory browserToolContextFactory,
        IIntegrationDefinitionService integrationDefinitionService,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser)
    {
        _agentMemoryService = memoryService;
        _agentCronJobRepository = cronJobRepository;
        _agentRunRepository = agentRunRepository;
        _agentTaskStore = taskStore;
        _browserToolContextFactory = browserToolContextFactory;
        _integrationDefinitionService = integrationDefinitionService;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
    }

    public async Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default)
    {
        var effectiveAgentId = agentId ?? Guid.Empty;
        var context = new ToolExecutionContext(effectiveAgentId, string.Empty, string.Empty, SchemaOnlyAgentSandbox.Instance);
        var tools = new List<IAgentTool>
        {
            new ShellTool(context),
            new FileReadTool(context),
            new FileWriteTool(context),
            new FileEditTool(context),
            new ContentSearchTool(context),
            new GlobSearchTool(context),
            new MemoryStoreTool(_agentMemoryService, effectiveAgentId),
            new MemoryRecallTool(_agentMemoryService, effectiveAgentId),
            new MemoryForgetTool(_agentMemoryService, effectiveAgentId),
            new AskUserQuestionTool(),
            new TaskCreateTool(_agentTaskStore, effectiveAgentId),
            new TaskListTool(_agentTaskStore, effectiveAgentId),
            new TaskGetTool(_agentTaskStore, effectiveAgentId),
            new TaskUpdateTool(_agentTaskStore, effectiveAgentId),
            new CronCreateTool(_agentCronJobRepository, effectiveAgentId),
            new CronListTool(_agentCronJobRepository, effectiveAgentId),
            new CronDeleteTool(_agentCronJobRepository, effectiveAgentId),
            new AgentSpawnTool(_agentRunRepository, effectiveAgentId),
            new HttpRequestTool(),
            new WebFetchTool(),
        };

        var browserContext = await _browserToolContextFactory.CreateCatalogAsync(ct);
        tools.AddRange(ToolRegistryFactory.CreateBrowserTools(browserContext, effectiveAgentId));

        AgentToolsetPermissionPolicy? toolsetPolicy = null;
        if (agentId.HasValue)
        {
            var definition = await _agentDefinitionRepository.GetByAsync(
                new AgentDefinitionFilter { AgentId = effectiveAgentId, ActiveOnly = true },
                ct);
            if (definition is not null)
                toolsetPolicy = new AgentToolsetPermissionPolicy(_agentDefinitionParser.Parse(definition.ConfigJson));
            if (toolsetPolicy is not null)
                tools = tools.Where(toolsetPolicy.IsAllowed).ToList();
        }

        var entries = tools.Select(ToEntry).ToList();

        if (agentId.HasValue)
        {
            var integrations = await _integrationDefinitionService.ListForAgentAsync(effectiveAgentId, ct: ct);
            foreach (var server in integrations)
            {
                foreach (var tool in ParseIntegrationTools(server))
                {
                    if (toolsetPolicy is null || toolsetPolicy.AllowsIntegrationTool(server.Name, tool.PermissionTool))
                        entries.Add(tool);
                }
            }
        }

        return entries.OrderBy(e => e.Group).ThenBy(e => e.RuntimeName).ToList();
    }

    private sealed class SchemaOnlyAgentSandbox : IAgentSandbox
    {
        public static readonly SchemaOnlyAgentSandbox Instance = new();

        private SchemaOnlyAgentSandbox()
        {
        }

        public Task<AgentSandboxDeployment> CreateAsync(
            Guid agentId,
            IReadOnlyDictionary<string, string> environment,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken ct = default)
            => throw new NotSupportedException("Tool catalog sandbox cannot create runtimes.");

        public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
            string sandboxId,
            string serviceUrl,
            string command,
            TimeSpan timeout,
            CancellationToken ct = default)
            => throw new NotSupportedException("Tool catalog sandbox cannot execute commands.");

        public Task<AgentResult<string>> ReadFileAsync(string sandboxId, string serviceUrl, string path, CancellationToken ct = default)
            => throw new NotSupportedException("Tool catalog sandbox cannot read files.");

        public Task<AgentResult<bool>> WriteFileAsync(string sandboxId, string serviceUrl, string path, string content, CancellationToken ct = default)
            => throw new NotSupportedException("Tool catalog sandbox cannot write files.");

        public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
            => throw new NotSupportedException("Tool catalog sandbox cannot terminate runtimes.");
    }

    private static AgentToolCatalogEntry ToEntry(IAgentTool tool)
    {
        var key = ToolKey.Parse(tool.PermissionScope);
        var group = key.SkillName == "browser" ? "browser" : "builtin";
        return new AgentToolCatalogEntry(
            group,
            tool.Name,
            key.SkillName,
            key.ToolName,
            tool.Schema.Description,
            tool.ShouldDefer);
    }

    private static IEnumerable<AgentToolCatalogEntry> ParseIntegrationTools(IntegrationDefinitionRecord server)
    {
        if (string.IsNullOrWhiteSpace(server.ToolsJson))
            yield break;

        JsonElement parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<JsonElement>(server.ToolsJson);
        }
        catch
        {
            yield break;
        }

        if (parsed.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in parsed.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var description = item.TryGetProperty("description", out var descProp)
                ? descProp.GetString() ?? name
                : name;
            yield return new AgentToolCatalogEntry(
                server.Name,
                $"{Slug(server.Name)}__{Slug(name)}",
                server.Name,
                name,
                $"[{server.Name}] {description}",
                true);
        }
    }

    private static string Slug(string value)
        => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

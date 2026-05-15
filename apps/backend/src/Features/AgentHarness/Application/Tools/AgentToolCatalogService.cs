namespace OffceOs.Application.Features.AgentHarness;

internal sealed class AgentToolCatalogService : IAgentToolCatalogService
{
    private readonly IAgentMemoryService _agentMemoryService;
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly IAgentRoutineService _agentRoutineService;
    private readonly AgentTaskStore _agentTaskStore;
    private readonly IBrowserToolService _browserToolService;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IAgentDefinitionRepository _agentDefinitionRepository;
    private readonly AgentDefinitionParser _agentDefinitionParser;
    private readonly IChannelService _channelService;
    private readonly IPublisher _publisher;
    private readonly AgentHarnessToolPermissionPolicy _agentHarnessToolPermissionPolicy;

    public AgentToolCatalogService(
        IAgentMemoryService memoryService,
        IAgentRoutineRepository agentRoutineRepository,
        IAgentRoutineService agentRoutineService,
        AgentTaskStore taskStore,
        IBrowserToolService browserToolService,
        IIntegrationDefinitionService integrationDefinitionService,
        IAgentDefinitionRepository agentDefinitionRepository,
        AgentDefinitionParser agentDefinitionParser,
        IChannelService channelService,
        IPublisher publisher,
        AgentHarnessToolPermissionPolicy agentHarnessToolPermissionPolicy)
    {
        _agentMemoryService = memoryService;
        _agentRoutineRepository = agentRoutineRepository;
        _agentRoutineService = agentRoutineService;
        _agentTaskStore = taskStore;
        _browserToolService = browserToolService;
        _integrationDefinitionService = integrationDefinitionService;
        _agentDefinitionRepository = agentDefinitionRepository;
        _agentDefinitionParser = agentDefinitionParser;
        _channelService = channelService;
        _publisher = publisher;
        _agentHarnessToolPermissionPolicy = agentHarnessToolPermissionPolicy;
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
            new RoutineCreateTool(_agentRoutineService, effectiveAgentId, null, null),
            new RoutineListTool(_agentRoutineRepository, effectiveAgentId),
            new RoutineDeleteTool(_agentRoutineRepository, effectiveAgentId),
            new AgentSpawnTool(_publisher, effectiveAgentId, null),
            new InternalChannelSendTool(_channelService, effectiveAgentId),
            new HttpRequestTool(),
            new WebFetchTool(),
        };

        tools.AddRange(await _browserToolService.CreateCatalogAsync(effectiveAgentId, ct));

        AgentToolsetPermissionPolicy? toolsetPolicy = null;
        if (agentId.HasValue)
        {
            var definition = await _agentDefinitionRepository.GetByAsync(
                new AgentDefinitionFilter { AgentId = effectiveAgentId, ActiveOnly = true },
                ct);
            if (definition is not null)
                toolsetPolicy = new AgentToolsetPermissionPolicy(
                    _agentDefinitionParser.Parse(definition.ConfigJson),
                    _agentHarnessToolPermissionPolicy);
            if (toolsetPolicy is not null)
                tools = tools.Where(tool => toolsetPolicy.IsAllowed(tool) || _agentHarnessToolPermissionPolicy.IsSelfManagementTool(tool)).ToList();
        }

        var entries = tools.Select(ToEntry).ToList();

        if (agentId.HasValue)
        {
            var integrations = await _integrationDefinitionService.ListForAgentAsync(effectiveAgentId, ct: ct);
            foreach (var server in integrations)
            {
                foreach (var tool in server.Tools)
                {
                    if (toolsetPolicy is null || toolsetPolicy.AllowsIntegrationTool(server.Name, tool.Name))
                    {
                        entries.Add(new AgentToolCatalogEntry(
                            server.Name,
                            $"{Slug(server.Name)}__{Slug(tool.Name)}",
                            server.Name,
                            tool.Name,
                            $"[{server.Name}] {tool.Description}",
                            true));
                    }
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

    private AgentToolCatalogEntry ToEntry(IAgentTool tool)
    {
        var key = ToolKey.Parse(_agentHarnessToolPermissionPolicy.ScopeFor(tool));
        var group = key.SkillName == "browser" ? "browser" : "builtin";
        return new AgentToolCatalogEntry(
            group,
            tool.Name,
            key.SkillName,
            key.ToolName,
            tool.Schema.Description,
            _agentHarnessToolPermissionPolicy.ShouldDefer(tool));
    }

    private static string Slug(string value)
        => new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
}

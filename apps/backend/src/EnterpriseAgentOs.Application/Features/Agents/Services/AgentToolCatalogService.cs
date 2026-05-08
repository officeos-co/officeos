namespace EnterpriseAgentOs.Application.Features.Agents;

public interface IAgentToolCatalogService
{
    Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default);
}

internal sealed class AgentToolCatalogService : IAgentToolCatalogService
{
    private readonly IAgentMemoryRepository _memoryRepo;
    private readonly IAgentResourceRepository _resourceRepository;
    private readonly IMemoryStoreRepository _memoryStoreRepository;
    private readonly IAgentCronJobRepository _cronJobRepository;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly AgentTaskStore _taskStore;
    private readonly IBrowserToolContextFactory _browserToolContextFactory;
    private readonly IMcpServerService _mcpServerService;
    private readonly IAgentToolPermissionRepository _permissionRepository;

    public AgentToolCatalogService(
        IAgentMemoryRepository memoryRepo,
        IAgentResourceRepository resourceRepository,
        IMemoryStoreRepository memoryStoreRepository,
        IAgentCronJobRepository cronJobRepository,
        IAgentRunRepository agentRunRepository,
        AgentTaskStore taskStore,
        IBrowserToolContextFactory browserToolContextFactory,
        IMcpServerService mcpServerService,
        IAgentToolPermissionRepository permissionRepository)
    {
        _memoryRepo = memoryRepo;
        _resourceRepository = resourceRepository;
        _memoryStoreRepository = memoryStoreRepository;
        _cronJobRepository = cronJobRepository;
        _agentRunRepository = agentRunRepository;
        _taskStore = taskStore;
        _browserToolContextFactory = browserToolContextFactory;
        _mcpServerService = mcpServerService;
        _permissionRepository = permissionRepository;
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
            new MemoryStoreTool(_memoryRepo, _resourceRepository, _memoryStoreRepository, effectiveAgentId),
            new MemoryRecallTool(_memoryRepo, _resourceRepository, _memoryStoreRepository, effectiveAgentId),
            new MemoryForgetTool(_memoryRepo, _resourceRepository, _memoryStoreRepository, effectiveAgentId),
            new AskUserQuestionTool(),
            new TaskCreateTool(_taskStore, effectiveAgentId),
            new TaskListTool(_taskStore, effectiveAgentId),
            new TaskGetTool(_taskStore, effectiveAgentId),
            new TaskUpdateTool(_taskStore, effectiveAgentId),
            new CronCreateTool(_cronJobRepository, effectiveAgentId),
            new CronListTool(_cronJobRepository, effectiveAgentId),
            new CronDeleteTool(_cronJobRepository, effectiveAgentId),
            new AgentSpawnTool(_agentRunRepository, effectiveAgentId),
            new HttpRequestTool(),
            new WebFetchTool(),
        };

        var browserContext = await _browserToolContextFactory.CreateCatalogAsync(ct);
        tools.AddRange(ToolRegistryFactory.CreateBrowserTools(browserContext, effectiveAgentId));

        if (agentId.HasValue)
        {
            var resolver = new AgentToolPermissionResolver(await _permissionRepository.ListForAgentAsync(effectiveAgentId, ct));
            tools = tools.Where(resolver.IsAllowed).ToList();
        }

        var entries = tools.Select(ToEntry).ToList();

        if (agentId.HasValue)
        {
            var mcpServers = await _mcpServerService.ListForAgentAsync(effectiveAgentId, ct);
            foreach (var server in mcpServers)
            {
                foreach (var tool in ParseMcpTools(server))
                    entries.Add(tool);
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

    private static IEnumerable<AgentToolCatalogEntry> ParseMcpTools(McpServerRecord server)
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

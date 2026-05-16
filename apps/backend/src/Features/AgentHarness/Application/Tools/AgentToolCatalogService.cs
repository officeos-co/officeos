using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.AgentRoutines;
using OffceOs.Application.Features.Context;
using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Application.Features.Channels;
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
    private readonly IChannelRepository _channelRepository;
    private readonly IPublisher _publisher;
    private readonly AgentHarnessToolPermissionPolicy _agentHarnessToolPermissionPolicy;
    private readonly AgentHarnessToolPermissionResolver _agentHarnessToolPermissionResolver;

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
        IChannelRepository channelRepository,
        IPublisher publisher,
        AgentHarnessToolPermissionPolicy agentHarnessToolPermissionPolicy,
        AgentHarnessToolPermissionResolver agentHarnessToolPermissionResolver)
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
        _channelRepository = channelRepository;
        _publisher = publisher;
        _agentHarnessToolPermissionPolicy = agentHarnessToolPermissionPolicy;
        _agentHarnessToolPermissionResolver = agentHarnessToolPermissionResolver;
    }

    public async Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default)
    {
        var effectiveAgentId = agentId ?? Guid.Empty;
        var context = new ToolExecutionContext(effectiveAgentId, string.Empty, string.Empty, SchemaOnlyAgentSandbox.Instance);
        var tools = new List<IAgentTool>();

        AgentToolsetPermissionPolicy? toolsetPolicy = null;
        var permissions = AgentHarnessResolvedToolPolicy.AllowAll();
        if (agentId.HasValue)
        {
            var definition = await _agentDefinitionRepository.GetByAsync(
                new AgentDefinitionFilter { AgentId = effectiveAgentId, ActiveOnly = true },
                ct);
            if (definition is not null)
            {
                var definitionConfig = _agentDefinitionParser.Parse(definition.ConfigJson);
                var canSendInternalChannel = await CanSendInternalChannelAsync(effectiveAgentId, ct);
                permissions = _agentHarnessToolPermissionResolver.Resolve(definitionConfig, canSendInternalChannel);
                toolsetPolicy = new AgentToolsetPermissionPolicy(
                    definitionConfig,
                    _agentHarnessToolPermissionPolicy);
            }
        }

        AddBuiltinCatalogTools(effectiveAgentId, context, tools, permissions);
        if (permissions.Browser)
        {
            tools.AddRange((await _browserToolService.CreateCatalogAsync(effectiveAgentId, ct))
                .Where(tool => permissions.AllowsBrowser(tool.Name)));
        }

        if (toolsetPolicy is not null)
            tools = tools.Where(tool => IsBuiltinTool(tool) || IsBrowserTool(tool) || toolsetPolicy.IsAllowed(tool)).ToList();

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

    private void AddBuiltinCatalogTools(
        Guid effectiveAgentId,
        ToolExecutionContext context,
        List<IAgentTool> tools,
        AgentHarnessResolvedToolPolicy permissions)
    {
        if (permissions.Shell)
            tools.Add(new ShellTool(context));
        if (permissions.FileRead)
            tools.Add(new FileReadTool(context));
        if (permissions.FileWrite)
            tools.Add(new FileWriteTool(context));
        if (permissions.FileEdit)
            tools.Add(new FileEditTool(context));
        if (permissions.ContentSearch)
            tools.Add(new ContentSearchTool(context));
        if (permissions.GlobSearch)
            tools.Add(new GlobSearchTool(context));
        if (permissions.MemoryStore)
            tools.Add(new MemoryStoreTool(_agentMemoryService, effectiveAgentId));
        if (permissions.MemoryRecall)
            tools.Add(new MemoryRecallTool(_agentMemoryService, effectiveAgentId));
        if (permissions.MemoryForget)
            tools.Add(new MemoryForgetTool(_agentMemoryService, effectiveAgentId));
        if (permissions.AskUserQuestion)
            tools.Add(new AskUserQuestionTool());
        if (permissions.TaskCreate)
            tools.Add(new TaskCreateTool(_agentTaskStore, effectiveAgentId));
        if (permissions.TaskList)
            tools.Add(new TaskListTool(_agentTaskStore, effectiveAgentId));
        if (permissions.TaskGet)
            tools.Add(new TaskGetTool(_agentTaskStore, effectiveAgentId));
        if (permissions.TaskUpdate)
            tools.Add(new TaskUpdateTool(_agentTaskStore, effectiveAgentId));
        if (permissions.RoutineCreate)
            tools.Add(new RoutineCreateTool(_agentRoutineService, effectiveAgentId, null, null));
        if (permissions.RoutineList)
            tools.Add(new RoutineListTool(_agentRoutineRepository, effectiveAgentId));
        if (permissions.RoutineDelete)
            tools.Add(new RoutineDeleteTool(_agentRoutineRepository, effectiveAgentId));
        if (permissions.AgentSpawn)
            tools.Add(new AgentSpawnTool(_publisher, effectiveAgentId, null));
        if (permissions.InternalChannelSend)
            tools.Add(new InternalChannelSendTool(_channelService, effectiveAgentId));
        if (permissions.HttpRequest)
            tools.Add(new HttpRequestTool());
        if (permissions.WebFetch)
            tools.Add(new WebFetchTool());
    }

    private async Task<bool> CanSendInternalChannelAsync(Guid agentId, CancellationToken ct)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);
        return bindings.Any(binding =>
            binding.Enabled
            && BindingAllowsSend(ChannelRoutingPolicy.ParseBindingConfig(binding.Config))
            && _agentHarnessToolPermissionResolver.ChannelPolicyAllows(
                binding.ChannelConnection?.ToolPermissionPolicyJson,
                "internal_channel_send"));
    }

    private static bool BindingAllowsSend(ChannelBindingConfig? config)
        => config?.CanSend ?? true;

    private static bool IsBuiltinTool(IAgentTool tool)
        => tool.Kind != AgentToolKind.Integration
            && !tool.Name.StartsWith("browser__", StringComparison.Ordinal);

    private static bool IsBrowserTool(IAgentTool tool)
        => tool.Name.StartsWith("browser__", StringComparison.Ordinal);
}

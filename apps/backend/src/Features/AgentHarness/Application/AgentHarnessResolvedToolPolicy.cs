namespace OffceOs.Application.Features.AgentHarness;

internal sealed class AgentHarnessResolvedToolPolicy
{
    public static readonly IReadOnlyList<string> BuiltinToolNames =
    [
        "shell",
        "file_read",
        "file_write",
        "file_edit",
        "content_search",
        "glob_search",
        "memory_store",
        "memory_recall",
        "memory_forget",
        "ask_user_question",
        "task_create",
        "task_list",
        "task_get",
        "task_update",
        "routine_create",
        "routine_list",
        "routine_delete",
        "agent_spawn",
        "http_request",
        "web_fetch",
    ];

    public static readonly IReadOnlyList<string> BrowserToolNames =
    [
        "browser__navigate",
        "browser__get_session",
        "browser__observe",
        "browser__screenshot",
        "browser__get_console",
        "browser__get_page_errors",
        "browser__get_request_failures",
        "browser__stop_trace",
        "browser__list_auth_profiles",
        "browser__get_auth_profile",
        "browser__list_downloads",
        "browser__list_tabs",
        "browser__activate_tab",
        "browser__close_tab",
        "browser__execute_action",
        "browser__save_auth_state",
        "browser__save_auth_profile",
        "browser__request_human_takeover",
        "browser__get_network_log",
        "browser__eval_js",
        "browser__wait_for_selector",
        "browser__get_html",
        "browser__find_elements",
        "browser__drag_drop",
        "browser__set_viewport",
        "browser__get_cookies",
        "browser__set_cookies",
        "browser__get_local_storage",
        "browser__set_local_storage",
        "browser__export_script",
        "browser__cdp_attach",
        "browser__find_by_vision",
    ];

    public static readonly IReadOnlyList<string> ChannelToolNames =
    [
        "internal_channel_send",
    ];

    public bool ToolSearch { get; init; } = true;
    public bool Shell { get; init; }
    public bool FileRead { get; init; }
    public bool FileWrite { get; init; }
    public bool FileEdit { get; init; }
    public bool ContentSearch { get; init; }
    public bool GlobSearch { get; init; }
    public bool MemoryStore { get; init; }
    public bool MemoryRecall { get; init; }
    public bool MemoryForget { get; init; }
    public bool AskUserQuestion { get; init; }
    public bool TaskCreate { get; init; }
    public bool TaskList { get; init; }
    public bool TaskGet { get; init; }
    public bool TaskUpdate { get; init; }
    public bool RoutineCreate { get; init; }
    public bool RoutineList { get; init; }
    public bool RoutineDelete { get; init; }
    public bool AgentSpawn { get; init; }
    public bool HttpRequest { get; init; }
    public bool WebFetch { get; init; }
    public bool Browser { get; init; }
    public bool BrowserNavigate { get; init; }
    public bool BrowserGetSession { get; init; }
    public bool BrowserObserve { get; init; }
    public bool BrowserScreenshot { get; init; }
    public bool BrowserGetConsole { get; init; }
    public bool BrowserGetPageErrors { get; init; }
    public bool BrowserGetRequestFailures { get; init; }
    public bool BrowserStopTrace { get; init; }
    public bool BrowserListAuthProfiles { get; init; }
    public bool BrowserGetAuthProfile { get; init; }
    public bool BrowserListDownloads { get; init; }
    public bool BrowserListTabs { get; init; }
    public bool BrowserActivateTab { get; init; }
    public bool BrowserCloseTab { get; init; }
    public bool BrowserExecuteAction { get; init; }
    public bool BrowserSaveAuthState { get; init; }
    public bool BrowserSaveAuthProfile { get; init; }
    public bool BrowserRequestHumanTakeover { get; init; }
    public bool BrowserGetNetworkLog { get; init; }
    public bool BrowserEvalJs { get; init; }
    public bool BrowserWaitForSelector { get; init; }
    public bool BrowserGetHtml { get; init; }
    public bool BrowserFindElements { get; init; }
    public bool BrowserDragDrop { get; init; }
    public bool BrowserSetViewport { get; init; }
    public bool BrowserGetCookies { get; init; }
    public bool BrowserSetCookies { get; init; }
    public bool BrowserGetLocalStorage { get; init; }
    public bool BrowserSetLocalStorage { get; init; }
    public bool BrowserExportScript { get; init; }
    public bool BrowserCdpAttach { get; init; }
    public bool BrowserFindByVision { get; init; }
    public bool InternalChannelSend { get; init; }

    public IReadOnlyList<string> DeniedBuiltinToolNames =>
        BuiltinToolNames.Where(toolName => !AllowsBuiltin(toolName)).ToList();

    public IReadOnlyList<string> DeniedBrowserToolNames =>
        BrowserToolNames.Where(toolName => !AllowsBrowser(toolName)).ToList();

    public IReadOnlyList<string> DeniedChannelToolNames =>
        ChannelToolNames.Where(toolName => !AllowsChannel(toolName)).ToList();

    public bool AllowsBuiltin(string toolName) => toolName switch
    {
        "shell" => Shell,
        "file_read" => FileRead,
        "file_write" => FileWrite,
        "file_edit" => FileEdit,
        "content_search" => ContentSearch,
        "glob_search" => GlobSearch,
        "memory_store" => MemoryStore,
        "memory_recall" => MemoryRecall,
        "memory_forget" => MemoryForget,
        "ask_user_question" => AskUserQuestion,
        "task_create" => TaskCreate,
        "task_list" => TaskList,
        "task_get" => TaskGet,
        "task_update" => TaskUpdate,
        "routine_create" => RoutineCreate,
        "routine_list" => RoutineList,
        "routine_delete" => RoutineDelete,
        "agent_spawn" => AgentSpawn,
        "http_request" => HttpRequest,
        "web_fetch" => WebFetch,
        _ => false,
    };

    public bool AllowsBrowser(string toolName) => toolName switch
    {
        "browser__navigate" => BrowserNavigate,
        "browser__get_session" => BrowserGetSession,
        "browser__observe" => BrowserObserve,
        "browser__screenshot" => BrowserScreenshot,
        "browser__get_console" => BrowserGetConsole,
        "browser__get_page_errors" => BrowserGetPageErrors,
        "browser__get_request_failures" => BrowserGetRequestFailures,
        "browser__stop_trace" => BrowserStopTrace,
        "browser__list_auth_profiles" => BrowserListAuthProfiles,
        "browser__get_auth_profile" => BrowserGetAuthProfile,
        "browser__list_downloads" => BrowserListDownloads,
        "browser__list_tabs" => BrowserListTabs,
        "browser__activate_tab" => BrowserActivateTab,
        "browser__close_tab" => BrowserCloseTab,
        "browser__execute_action" => BrowserExecuteAction,
        "browser__save_auth_state" => BrowserSaveAuthState,
        "browser__save_auth_profile" => BrowserSaveAuthProfile,
        "browser__request_human_takeover" => BrowserRequestHumanTakeover,
        "browser__get_network_log" => BrowserGetNetworkLog,
        "browser__eval_js" => BrowserEvalJs,
        "browser__wait_for_selector" => BrowserWaitForSelector,
        "browser__get_html" => BrowserGetHtml,
        "browser__find_elements" => BrowserFindElements,
        "browser__drag_drop" => BrowserDragDrop,
        "browser__set_viewport" => BrowserSetViewport,
        "browser__get_cookies" => BrowserGetCookies,
        "browser__set_cookies" => BrowserSetCookies,
        "browser__get_local_storage" => BrowserGetLocalStorage,
        "browser__set_local_storage" => BrowserSetLocalStorage,
        "browser__export_script" => BrowserExportScript,
        "browser__cdp_attach" => BrowserCdpAttach,
        "browser__find_by_vision" => BrowserFindByVision,
        _ => false,
    };

    public bool AllowsChannel(string toolName) => toolName switch
    {
        "internal_channel_send" => InternalChannelSend,
        _ => false,
    };

    public static AgentHarnessResolvedToolPolicy AllowAll() => Create(static _ => true, static _ => true, static _ => true);

    public static AgentHarnessResolvedToolPolicy Create(
        Func<string, bool> allowsBuiltin,
        Func<string, bool> allowsBrowser,
        Func<string, bool> allowsChannel)
        => new()
        {
            Shell = allowsBuiltin("shell"),
            FileRead = allowsBuiltin("file_read"),
            FileWrite = allowsBuiltin("file_write"),
            FileEdit = allowsBuiltin("file_edit"),
            ContentSearch = allowsBuiltin("content_search"),
            GlobSearch = allowsBuiltin("glob_search"),
            MemoryStore = allowsBuiltin("memory_store"),
            MemoryRecall = allowsBuiltin("memory_recall"),
            MemoryForget = allowsBuiltin("memory_forget"),
            AskUserQuestion = allowsBuiltin("ask_user_question"),
            TaskCreate = allowsBuiltin("task_create"),
            TaskList = allowsBuiltin("task_list"),
            TaskGet = allowsBuiltin("task_get"),
            TaskUpdate = allowsBuiltin("task_update"),
            RoutineCreate = true,
            RoutineList = true,
            RoutineDelete = true,
            AgentSpawn = allowsBuiltin("agent_spawn"),
            HttpRequest = allowsBuiltin("http_request"),
            WebFetch = allowsBuiltin("web_fetch"),
            BrowserNavigate = allowsBrowser("browser__navigate"),
            BrowserGetSession = allowsBrowser("browser__get_session"),
            BrowserObserve = allowsBrowser("browser__observe"),
            BrowserScreenshot = allowsBrowser("browser__screenshot"),
            BrowserGetConsole = allowsBrowser("browser__get_console"),
            BrowserGetPageErrors = allowsBrowser("browser__get_page_errors"),
            BrowserGetRequestFailures = allowsBrowser("browser__get_request_failures"),
            BrowserStopTrace = allowsBrowser("browser__stop_trace"),
            BrowserListAuthProfiles = allowsBrowser("browser__list_auth_profiles"),
            BrowserGetAuthProfile = allowsBrowser("browser__get_auth_profile"),
            BrowserListDownloads = allowsBrowser("browser__list_downloads"),
            BrowserListTabs = allowsBrowser("browser__list_tabs"),
            BrowserActivateTab = allowsBrowser("browser__activate_tab"),
            BrowserCloseTab = allowsBrowser("browser__close_tab"),
            BrowserExecuteAction = allowsBrowser("browser__execute_action"),
            BrowserSaveAuthState = allowsBrowser("browser__save_auth_state"),
            BrowserSaveAuthProfile = allowsBrowser("browser__save_auth_profile"),
            BrowserRequestHumanTakeover = allowsBrowser("browser__request_human_takeover"),
            BrowserGetNetworkLog = allowsBrowser("browser__get_network_log"),
            BrowserEvalJs = allowsBrowser("browser__eval_js"),
            BrowserWaitForSelector = allowsBrowser("browser__wait_for_selector"),
            BrowserGetHtml = allowsBrowser("browser__get_html"),
            BrowserFindElements = allowsBrowser("browser__find_elements"),
            BrowserDragDrop = allowsBrowser("browser__drag_drop"),
            BrowserSetViewport = allowsBrowser("browser__set_viewport"),
            BrowserGetCookies = allowsBrowser("browser__get_cookies"),
            BrowserSetCookies = allowsBrowser("browser__set_cookies"),
            BrowserGetLocalStorage = allowsBrowser("browser__get_local_storage"),
            BrowserSetLocalStorage = allowsBrowser("browser__set_local_storage"),
            BrowserExportScript = allowsBrowser("browser__export_script"),
            BrowserCdpAttach = allowsBrowser("browser__cdp_attach"),
            BrowserFindByVision = allowsBrowser("browser__find_by_vision"),
            Browser = BrowserToolNames.Any(allowsBrowser),
            InternalChannelSend = allowsChannel("internal_channel_send"),
        };
}

internal sealed class AgentHarnessToolPermissionResolver
{
    public AgentHarnessResolvedToolPolicy Resolve(AgentDefinitionConfig definitionConfig, bool canSendInternalChannel = false)
    {
        var builtinToolset = definitionConfig.Tools.FirstOrDefault(toolset => toolset.Type == AgentToolsetKinds.Builtin);
        var browserToolset = definitionConfig.Tools.FirstOrDefault(toolset => toolset.Type == AgentToolsetKinds.Browser);

        return AgentHarnessResolvedToolPolicy.Create(
            toolName => builtinToolset is not null && PolicyAllows("builtin", toolName, toolName, builtinToolset),
            toolName => browserToolset is not null && PolicyAllowsBrowser(toolName, browserToolset),
            toolName => toolName is "internal_channel_send" && canSendInternalChannel);
    }

    private static bool PolicyAllows(string groupName, string toolName, string runtimeName, AgentToolsetConfig toolset)
    {
        var policy = toolset.DefaultConfig?.PermissionPolicy
            ?? new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null);

        return policy.Type switch
        {
            AgentToolPermissionKinds.AlwaysAllow => true,
            AgentToolPermissionKinds.AlwaysDeny => false,
            AgentToolPermissionKinds.AllowList => Matches(policy.Tools, groupName, toolName, runtimeName),
            AgentToolPermissionKinds.DenyList => !Matches(policy.Tools, groupName, toolName, runtimeName),
            _ => false,
        };
    }

    public bool ChannelPolicyAllows(string? policyJson, string toolName)
        => string.IsNullOrWhiteSpace(policyJson)
            || PolicyAllows("channel", toolName, toolName, ParsePolicy(policyJson));

    private static bool PolicyAllowsBrowser(string toolName, AgentToolsetConfig toolset)
        => PolicyAllows(
            "browser",
            ShortBrowserToolName(toolName),
            toolName,
            toolset.DefaultConfig?.PermissionPolicy ?? new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null),
            BrowserRuntimeName(toolName));

    private static bool PolicyAllows(string groupName, string toolName, string runtimeName, AgentToolPermissionConfig policy, params string[] aliases)
        => policy.Type switch
        {
            AgentToolPermissionKinds.AlwaysAllow => true,
            AgentToolPermissionKinds.AlwaysDeny => false,
            AgentToolPermissionKinds.AllowList => Matches(policy.Tools, groupName, toolName, runtimeName, aliases),
            AgentToolPermissionKinds.DenyList => !Matches(policy.Tools, groupName, toolName, runtimeName, aliases),
            _ => false,
        };

    private static AgentToolPermissionConfig ParsePolicy(string policyJson)
    {
        var policy = JsonSerializer.Deserialize<AgentToolPermissionConfig>(policyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        return policy ?? new AgentToolPermissionConfig(AgentToolPermissionKinds.AlwaysAllow, null);
    }

    private static bool Matches(IReadOnlyList<string>? patterns, string groupName, string toolName, string runtimeName, params string[] aliases)
    {
        if (patterns is null)
            return false;

        var scope = $"{groupName}:{toolName}";
        var scopedRuntime = $"{groupName}:{runtimeName}";
        return patterns.Any(pattern =>
            string.Equals(pattern, toolName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pattern, runtimeName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pattern, scope, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pattern, scopedRuntime, StringComparison.OrdinalIgnoreCase)
            || aliases.Any(alias => string.Equals(pattern, alias, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pattern, $"{groupName}:{alias}", StringComparison.OrdinalIgnoreCase)));
    }

    private static string BrowserRuntimeName(string toolName)
        => toolName.Replace("__", ".");

    private static string ShortBrowserToolName(string toolName)
        => toolName.StartsWith("browser__", StringComparison.Ordinal)
            ? toolName["browser__".Length..]
            : toolName;
}

using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Sandbox;

public sealed class BrowserToolTests
{
    [Fact]
    public async Task Browser_tool_factory_registers_every_named_browser_tool()
    {
        var contextFactory = new BrowserToolContextFactory(new FakeBrowserService(), new FakeBrowserRuntimeClient());
        var context = await contextFactory.CreateCatalogAsync();
        var tools = ToolRegistryFactory.CreateBrowserTools(context, Guid.NewGuid());
        var names = tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(32, names.Count);
        Assert.Contains("browser__navigate", names);
        Assert.Contains("browser__get_session", names);
        Assert.Contains("browser__observe", names);
        Assert.Contains("browser__screenshot", names);
        Assert.Contains("browser__get_console", names);
        Assert.Contains("browser__get_page_errors", names);
        Assert.Contains("browser__get_request_failures", names);
        Assert.Contains("browser__stop_trace", names);
        Assert.Contains("browser__list_auth_profiles", names);
        Assert.Contains("browser__get_auth_profile", names);
        Assert.Contains("browser__list_downloads", names);
        Assert.Contains("browser__list_tabs", names);
        Assert.Contains("browser__activate_tab", names);
        Assert.Contains("browser__close_tab", names);
        Assert.Contains("browser__execute_action", names);
        Assert.Contains("browser__save_auth_state", names);
        Assert.Contains("browser__save_auth_profile", names);
        Assert.Contains("browser__request_human_takeover", names);
        Assert.Contains("browser__get_network_log", names);
        Assert.Contains("browser__eval_js", names);
        Assert.Contains("browser__wait_for_selector", names);
        Assert.Contains("browser__get_html", names);
        Assert.Contains("browser__find_elements", names);
        Assert.Contains("browser__drag_drop", names);
        Assert.Contains("browser__set_viewport", names);
        Assert.Contains("browser__get_cookies", names);
        Assert.Contains("browser__set_cookies", names);
        Assert.Contains("browser__get_local_storage", names);
        Assert.Contains("browser__set_local_storage", names);
        Assert.Contains("browser__export_script", names);
        Assert.Contains("browser__cdp_attach", names);
        Assert.Contains("browser__find_by_vision", names);
    }

    [Fact]
    public void Navigate_tool_is_always_loaded()
    {
        var tool = new BrowserNavigateTool(new FakeBrowserService(), new FakeBrowserRuntimeClient(), Guid.NewGuid());

        Assert.Equal("browser__navigate", tool.Name);
        Assert.True(tool.AlwaysLoad);
        Assert.False(tool.ShouldDefer);
    }

    [Fact]
    public async Task Navigate_tool_executes_browser_navigate_action()
    {
        var runtime = new FakeBrowserRuntimeClient();
        var tool = new BrowserNavigateTool(new FakeBrowserService(), runtime, Guid.NewGuid());
        var args = JsonSerializer.SerializeToElement(new
        {
            url = "https://news.ycombinator.com/",
            reason = "Open Hacker News."
        });

        var result = await tool.ExecuteAsync(args, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("browser.execute_action", runtime.LastToolName);
        Assert.Equal("session-1", runtime.LastArguments?["session_id"]);
        var action = Assert.IsType<Dictionary<string, object?>>(runtime.LastArguments?["action"]);
        Assert.Equal("navigate", action["action"]);
        Assert.Equal("https://news.ycombinator.com/", action["url"]);
        Assert.Equal("read", action["risk_category"]);
    }

    [Fact]
    public async Task Initial_schema_list_includes_all_browser_tools_without_search()
    {
        var contextFactory = new BrowserToolContextFactory(new FakeBrowserService(), new FakeBrowserRuntimeClient());
        var context = await contextFactory.CreateCatalogAsync();
        var tools = ToolRegistryFactory.CreateBrowserTools(context, Guid.NewGuid()).ToList();
        await using var registry = new ToolRegistry(
            tools,
            new ToolExecutionContext(Guid.NewGuid(), "sandbox-1", "http://sandbox", new FakeAgentSandbox()),
            preloadedToolNames: tools.Select(t => t.Name));

        var schemas = JsonSerializer.Serialize(registry.GetSchemas());
        var deferredTools = registry.GetDeferredToolsMessage();

        Assert.Contains("browser__navigate", schemas);
        Assert.Contains("browser__execute_action", schemas);
        Assert.Contains("browser__get_console", schemas);
        Assert.DoesNotContain("group: browser", deferredTools);
    }

    private sealed class FakeBrowserService : IBrowserService
    {
        public Task<BrowserSessionState> GetOrCreateAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult(new BrowserSessionState(agentId, "session-1", "active", null, null, null, null, null, null));

        public Task<BrowserSessionState?> GetStateAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult<BrowserSessionState?>(null);

        public Task<BrowserSessionState> RestartAsync(Guid agentId, CancellationToken ct = default)
            => GetOrCreateAsync(agentId, ct);

        public Task StopAsync(Guid agentId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<string?> GetViewUrlAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeBrowserRuntimeClient : IBrowserRuntimeClient
    {
        public string? LastToolName { get; private set; }
        public Dictionary<string, object?>? LastArguments { get; private set; }

        public Task<bool> IsAvailableAsync(CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<BrowserSessionState?> GetSessionAsync(Guid agentId, string runtimeSessionId, CancellationToken ct = default)
            => Task.FromResult<BrowserSessionState?>(null);

        public Task<BrowserSessionState> CreateSessionAsync(Guid agentId, string name, string? authProfile, CancellationToken ct = default)
            => Task.FromResult(new BrowserSessionState(agentId, "session-1", "active", name, null, null, null, null, null));

        public Task CloseSessionAsync(string runtimeSessionId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BrowserToolDescriptor>> ListToolsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BrowserToolDescriptor>>([]);

        public Task<BrowserToolCallResult> CallToolAsync(string name, Dictionary<string, object?> arguments, CancellationToken ct = default)
        {
            LastToolName = name;
            LastArguments = arguments;
            return Task.FromResult(new BrowserToolCallResult(false, "{}"));
        }
    }

    private sealed class FakeAgentSandbox : IAgentSandbox
    {
        public Task<AgentSandboxDeployment> CreateAsync(
            Guid agentId,
            IReadOnlyDictionary<string, string> environment,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
            string sandboxId,
            string serviceUrl,
            string command,
            TimeSpan timeout,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AgentResult<string>> ReadFileAsync(string sandboxId, string serviceUrl, string path, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AgentResult<bool>> WriteFileAsync(string sandboxId, string serviceUrl, string path, string content, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}

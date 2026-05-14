using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Sandbox;

public sealed class BrowserToolTests
{
    [Fact]
    public async Task Browser_tool_factory_registers_every_named_browser_tool()
    {
        var browserToolService = new BrowserToolService(new BrowserToolContextFactory(
            new FakeBrowserService(),
            new FakeBrowserRuntimeClient()));
        var tools = await browserToolService.CreateCatalogAsync(Guid.NewGuid());
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
        var browserToolService = new BrowserToolService(new BrowserToolContextFactory(
            new FakeBrowserService(),
            new FakeBrowserRuntimeClient()));
        var tools = (await browserToolService.CreateCatalogAsync(Guid.NewGuid())).ToList();
        await using var registry = new ToolRegistry(new ToolRegistryContext
        {
            Tools = tools,
            ToolExecutionContext = new ToolExecutionContext(Guid.NewGuid(), "sandbox-1", "http://sandbox", new FakeAgentSandbox()),
            IntegrationConnections = [],
            PreloadedToolNames = tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal),
            PolicyDeniedToolReasons = new Dictionary<string, string>(StringComparer.Ordinal),
            TurnEventPublisher = new TurnEventPublisher(new NoopPublisher()),
            CorrelationId = "correlation",
        });

        var schemas = JsonSerializer.Serialize(registry.GetSchemas());
        var deferredTools = registry.GetDeferredToolsMessage();

        Assert.Contains("browser__navigate", schemas);
        Assert.Contains("browser__execute_action", schemas);
        Assert.Contains("browser__get_console", schemas);
        Assert.DoesNotContain("group: browser", deferredTools);
    }

}

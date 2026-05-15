namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserToolService : IBrowserToolService
{
    private readonly IBrowserToolContextFactory _browserToolContextFactory;

    public BrowserToolService(IBrowserToolContextFactory browserToolContextFactory)
    {
        _browserToolContextFactory = browserToolContextFactory;
    }

    public async Task<IReadOnlyList<IAgentTool>> CreateForTurnAsync(Guid agentId, CancellationToken ct = default)
    {
        var browser = await _browserToolContextFactory.CreateForTurnAsync(ct);
        return browser is null ? [] : Create(browser, agentId);
    }

    public async Task<IReadOnlyList<IAgentTool>> CreateCatalogAsync(Guid agentId, CancellationToken ct = default)
    {
        var browser = await _browserToolContextFactory.CreateCatalogAsync(ct);
        return Create(browser, agentId);
    }

    private static IReadOnlyList<IAgentTool> Create(BrowserToolContext browser, Guid agentId)
        =>
        [
            new BrowserNavigateTool(browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetSessionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserObserveTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserScreenshotTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetConsoleTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetPageErrorsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetRequestFailuresTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserStopTraceTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListAuthProfilesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetAuthProfileTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListDownloadsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserListTabsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserActivateTabTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserCloseTabTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserExecuteActionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSaveAuthStateTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSaveAuthProfileTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserRequestHumanTakeoverTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetNetworkLogTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserEvalJsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserWaitForSelectorTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetHtmlTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserFindElementsTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserDragDropTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetViewportTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetCookiesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetCookiesTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserGetLocalStorageTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserSetLocalStorageTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserExportScriptTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserCdpAttachTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
            new BrowserFindByVisionTool(browser.Descriptors, browser.BrowserService, browser.BrowserRuntime, agentId),
        ];
}

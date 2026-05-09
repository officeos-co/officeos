namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserCloseTabTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.close_tab";
    private const string Description = "Close one tab index if more than one tab is open.";
}

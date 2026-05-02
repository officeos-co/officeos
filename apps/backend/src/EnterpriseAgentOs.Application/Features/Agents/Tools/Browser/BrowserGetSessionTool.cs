namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserGetSessionTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_session";
    private const string Description = "Get one browser session summary.";
}

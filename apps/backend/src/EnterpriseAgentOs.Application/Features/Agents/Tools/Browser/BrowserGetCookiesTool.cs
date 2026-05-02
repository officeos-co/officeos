namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserGetCookiesTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_cookies";
    private const string Description = "Get all cookies for the current session context.";
}

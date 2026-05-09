namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserSetCookiesTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.set_cookies";
    private const string Description = "Set one or more cookies in the current session context.";
}

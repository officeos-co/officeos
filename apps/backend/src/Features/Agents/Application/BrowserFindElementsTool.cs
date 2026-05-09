namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserFindElementsTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.find_elements";
    private const string Description = "Find all elements matching a CSS selector and return text, href, value, bounding box, and visibility.";
}

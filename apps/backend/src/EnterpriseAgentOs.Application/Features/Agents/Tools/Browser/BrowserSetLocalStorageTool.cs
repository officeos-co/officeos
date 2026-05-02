namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserSetLocalStorageTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.set_local_storage";
    private const string Description = "Write localStorage or sessionStorage in the current page context.";
}

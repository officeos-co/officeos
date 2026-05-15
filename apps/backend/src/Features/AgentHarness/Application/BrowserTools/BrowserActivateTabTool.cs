namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserActivateTabTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.activate_tab";
    private const string Description = "Switch the active session page to one tab index.";
}

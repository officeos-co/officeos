namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserWaitForSelectorTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.wait_for_selector";
    private const string Description = "Wait for a CSS selector to reach a specific state.";
}

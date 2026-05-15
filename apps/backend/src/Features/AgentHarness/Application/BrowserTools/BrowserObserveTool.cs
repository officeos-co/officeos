namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserObserveTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.observe";
    private const string Description = "Capture the current browser observation with screenshot, interactables, and perception summary.";
}

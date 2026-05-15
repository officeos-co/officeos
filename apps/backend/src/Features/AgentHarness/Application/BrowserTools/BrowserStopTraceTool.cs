namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserStopTraceTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.stop_trace";
    private const string Description = "Finalize the current Playwright trace for an active session and return its artifact path.";
}

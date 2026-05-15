namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserRequestHumanTakeoverTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.request_human_takeover";
    private const string Description = "Ask for a human to take over the shared browser desktop.";
}

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserGetNetworkLogTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_network_log";
    private const string Description = "Return captured HTTP request/response entries for a session.";
}

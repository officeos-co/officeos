namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserGetPageErrorsTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_page_errors";
    private const string Description = "Read recent uncaught page errors for an active session.";
}

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserListDownloadsTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.list_downloads";
    private const string Description = "List files captured from browser downloads for one session.";
}

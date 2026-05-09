namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserCdpAttachTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.cdp_attach";
    private const string Description = "Attach to an already-running Chrome instance via CDP URL.";
}

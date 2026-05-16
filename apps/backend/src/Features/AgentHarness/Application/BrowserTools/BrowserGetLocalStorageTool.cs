using OffceOs.Features.Browser.Domain;

namespace OffceOs.Features.AgentHarness.Application.BrowserTools;

internal sealed class BrowserGetLocalStorageTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_local_storage";
    private const string Description = "Read localStorage or sessionStorage in the current page context.";
}

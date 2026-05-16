using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserGetLocalStorageTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_local_storage";
    private const string Description = "Read localStorage or sessionStorage in the current page context.";
}

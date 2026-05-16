using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserSetViewportTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.set_viewport";
    private const string Description = "Resize the browser viewport to the specified width and height.";
}

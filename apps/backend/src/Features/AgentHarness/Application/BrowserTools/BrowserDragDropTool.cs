using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserDragDropTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.drag_drop";
    private const string Description = "Drag from one element or coordinate to another.";
}

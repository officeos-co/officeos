namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserDragDropTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.drag_drop";
    private const string Description = "Drag from one element or coordinate to another.";
}

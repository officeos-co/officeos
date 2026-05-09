namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserFindByVisionTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.find_by_vision";
    private const string Description = "Find an element from a natural language description and return click coordinates.";
}

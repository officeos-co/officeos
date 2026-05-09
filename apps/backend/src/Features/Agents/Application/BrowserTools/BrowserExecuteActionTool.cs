namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserExecuteActionTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.execute_action";
    private const string Description = "Execute one browser action using the shared internal action schema.";
}

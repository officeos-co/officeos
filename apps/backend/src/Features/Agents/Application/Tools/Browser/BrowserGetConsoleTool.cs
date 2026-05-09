namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserGetConsoleTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_console";
    private const string Description = "Read recent browser console messages for an active session.";
}

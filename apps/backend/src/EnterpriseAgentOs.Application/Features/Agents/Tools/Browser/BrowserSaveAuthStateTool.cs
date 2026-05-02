namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserSaveAuthStateTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.save_auth_state";
    private const string Description = "Save session storage state to the per-session auth-state root.";
}

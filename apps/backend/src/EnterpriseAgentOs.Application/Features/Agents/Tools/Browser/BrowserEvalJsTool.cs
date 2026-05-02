namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class BrowserEvalJsTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserMcpTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.eval_js";
    private const string Description = "Execute a JavaScript expression in the current page context and return the result.";
}

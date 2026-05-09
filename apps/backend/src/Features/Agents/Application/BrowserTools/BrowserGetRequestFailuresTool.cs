namespace OffceOs.Application.Features.Agents;

internal sealed class BrowserGetRequestFailuresTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_request_failures";
    private const string Description = "Read recent failed network requests for an active session.";
}

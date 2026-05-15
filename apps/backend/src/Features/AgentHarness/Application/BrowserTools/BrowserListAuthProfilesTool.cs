namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserListAuthProfilesTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.list_auth_profiles";
    private const string Description = "List reusable saved auth profiles that can be loaded into a new session.";
}

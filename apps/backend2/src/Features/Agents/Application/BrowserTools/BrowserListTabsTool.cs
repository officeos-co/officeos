namespace OffceOs.Application.Features.Agents;

internal sealed class BrowserListTabsTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.list_tabs";
    private const string Description = "List currently open tabs/pages for one session.";
}

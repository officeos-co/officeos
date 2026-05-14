namespace OffceOs.Application.Features.Agents;

internal sealed class BrowserGetHtmlTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_html";
    private const string Description = "Get the HTML source of the current page, optionally as plain text.";
}

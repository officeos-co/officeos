namespace OffceOs.Application.Features.Agents;

internal sealed class BrowserScreenshotTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.screenshot";
    private const string Description = "Capture a lightweight screenshot for one session without the full observe payload.";
}

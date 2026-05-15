namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserExportScriptTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.export_script";
    private const string Description = "Export the current session's recorded actions as a runnable Playwright Python script.";
}

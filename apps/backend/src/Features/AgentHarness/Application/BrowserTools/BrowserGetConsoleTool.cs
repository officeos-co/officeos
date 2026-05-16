using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.AgentHarness;

internal sealed class BrowserGetConsoleTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_console";
    private const string Description = "Read recent browser console messages for an active session.";
}

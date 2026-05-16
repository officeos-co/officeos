using OffceOs.Features.Browser.Domain;

namespace OffceOs.Features.AgentHarness.Application.BrowserTools;

internal sealed class BrowserGetAuthProfileTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.get_auth_profile";
    private const string Description = "Inspect one saved auth profile and its storage-state metadata.";
}

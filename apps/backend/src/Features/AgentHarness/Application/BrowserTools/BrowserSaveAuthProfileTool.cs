using OffceOs.Features.Browser.Domain;

namespace OffceOs.Features.AgentHarness.Application.BrowserTools;

internal sealed class BrowserSaveAuthProfileTool(IReadOnlyDictionary<string, BrowserToolDescriptor> descriptors, IBrowserService browser, IBrowserRuntimeClient runtime, Guid agentId)
    : BrowserRuntimeTool(RuntimeName, Description, descriptors, browser, runtime, agentId)
{
    public const string RuntimeName = "browser.save_auth_profile";
    private const string Description = "Save the current session storage state into a reusable named auth profile.";
}

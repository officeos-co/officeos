using EnterpriseAgentOs.Domain.Features.Agents;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class AgentPersonalityRecordTests
{
    [Fact]
    public void CreateBootstrapContent_preserves_default_guidance_when_user_prompt_is_added()
    {
        var content = AgentPersonalityRecord.CreateBootstrapContent("Prefer concise answers.");

        Assert.Contains("# Bootstrap", content);
        Assert.Contains("Read/search before assuming.", content);
        Assert.Contains("## User Bootstrap", content);
        Assert.Contains("Prefer concise answers.", content);
    }
}

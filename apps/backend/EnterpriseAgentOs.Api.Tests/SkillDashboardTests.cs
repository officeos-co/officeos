namespace EnterpriseAgentOs.Api.Tests;

public sealed class SkillDashboardTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;
    public SkillDashboardTests(Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    private void SeedManifests()
    {
        var manifest = """
        [
          {
            "name": "github",
            "title": "GitHub",
            "logo": "<svg viewBox=\"0 0 24 24\"><path d=\"M12 0C5.37 0 0 5.37 0 12\"/></svg>",
            "description": "GitHub integration",
            "doc": "GitHub skill docs",
            "actions": {
              "create_issue": {
                "description": "Create a GitHub issue",
                "params": { "type": "object", "properties": { "repo": { "type": "string" } } }
              }
            },
            "credentialFields": [
              { "key": "token", "label": "Token", "kind": "password", "required": true }
            ]
          }
        ]
        """;
        _factory.SkillRuntimeMock.Reset();
        _factory.SkillRuntimeMock
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(manifest));
    }

    [Fact]
    public async Task Skills_Query_Returns_Logo_Not_Emoji()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ skills { name logo sourceCodeUrl } }");
        var skills = data.GetProperty("skills");
        Assert.True(skills.GetArrayLength() > 0);
        var skill = skills.EnumerateArray().First(s => s.GetProperty("name").GetString() == "github");
        var logo = skill.GetProperty("logo").GetString();
        Assert.NotNull(logo);
        Assert.StartsWith("<svg", logo);
        var url = skill.GetProperty("sourceCodeUrl").GetString();
        Assert.Equal("https://github.com/officeos-co/skill-github", url);
    }

    [Fact]
    public async Task Skills_Query_Has_No_Emoji_Field()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");
        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, "{ skills { name emoji } }");
        Assert.True(raw.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Skills_Query_Returns_Installed_Status()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ skills { name installed } }");
        var skill = data.GetProperty("skills").EnumerateArray().First(s => s.GetProperty("name").GetString() == "github");
        Assert.True(skill.GetProperty("installed").GetBoolean());
    }

    [Fact]
    public async Task ChannelTypes_Query_Returns_Logo()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ channelTypes { type displayName logo } }");
        var types = data.GetProperty("channelTypes");
        Assert.True(types.GetArrayLength() >= 7);
        var slack = types.EnumerateArray().First(t => t.GetProperty("type").GetString() == "slack");
        var logo = slack.GetProperty("logo").GetString();
        Assert.NotNull(logo);
        Assert.StartsWith("<svg", logo);
    }
}

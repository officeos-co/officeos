namespace EnterpriseAgentOs.Api.Tests;

public sealed class SkillDashboardTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _customWebApplicationFactory;
    public SkillDashboardTests(Infrastructure.CustomWebApplicationFactory factory) => _customWebApplicationFactory = factory;

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
        _customWebApplicationFactory.SkillRuntimeMock.Reset();
        _customWebApplicationFactory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(manifest));
    }

    [Fact]
    public async Task Skills_Query_Returns_Logo_Not_Emoji()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
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
    public async Task Skills_Query_Returns_Typed_Manifest_Fields()
    {
        _customWebApplicationFactory.SkillRuntimeMock.Reset();
        _customWebApplicationFactory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                [
                  {
                    "name": "stock-analysis",
                    "title": "Stock Analysis",
                    "logo": "<svg viewBox=\"0 0 24 24\"><path d=\"M12 0\"/></svg>",
                    "description": "Fetch stock data",
                    "doc": "Stock skill docs",
                    "license": "MIT",
                    "repository": "https://github.com/officeos-co/skill-stock-analysis",
                    "categories": ["Finance", "Data & Analytics"],
                    "keywords": ["stocks", "finance"],
                    "author": { "name": "OfficeOS Team", "url": "https://officeos.co" },
                    "contributors": [{ "name": "Harro Krog", "url": "https://github.com/HarKro753" }],
                    "requiresApproval": false,
                    "actions": {
                      "quote": {
                        "description": "Get a real-time quote",
                        "params": { "type": "object", "properties": { "symbol": { "type": "string" } } }
                      }
                    },
                    "credentialFields": [
                      { "key": "proxy_url", "label": "Proxy URL", "kind": "text", "required": false }
                    ]
                  }
                ]
                """));

        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "stock-analysis");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, @"
            { skills {
                name logo license repository categories keywords
                requiresApproval
                author { name url }
                contributors { name url }
                tools { name description }
            }}");

        var skills = data.GetProperty("skills");
        var skill = skills.EnumerateArray().First(s => s.GetProperty("name").GetString() == "stock-analysis");

        // Scalar columns
        Assert.StartsWith("<svg", skill.GetProperty("logo").GetString());
        Assert.Equal("MIT", skill.GetProperty("license").GetString());
        Assert.Equal("https://github.com/officeos-co/skill-stock-analysis", skill.GetProperty("repository").GetString());
        Assert.False(skill.GetProperty("requiresApproval").GetBoolean());

        // Array columns
        var categories = skill.GetProperty("categories").EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.Contains("Finance", categories);
        Assert.Contains("Data & Analytics", categories);
        var keywords = skill.GetProperty("keywords").EnumerateArray().Select(k => k.GetString()).ToList();
        Assert.Contains("stocks", keywords);

        // Author (from typed columns)
        var author = skill.GetProperty("author");
        Assert.Equal("OfficeOS Team", author.GetProperty("name").GetString());
        Assert.Equal("https://officeos.co", author.GetProperty("url").GetString());

        // Contributors (from JSONB column)
        var contributors = skill.GetProperty("contributors");
        Assert.Equal(1, contributors.GetArrayLength());
        Assert.Equal("Harro Krog", contributors[0].GetProperty("name").GetString());

        // Actions (from JSONB column)
        var tools = skill.GetProperty("tools");
        Assert.True(tools.GetArrayLength() > 0);
        Assert.Equal("quote", tools[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Skills_Query_Has_No_Emoji_Field()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");
        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, "{ skills { name emoji } }");
        Assert.True(raw.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Skills_Query_Returns_Installed_Status()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ skills { name installed } }");
        var skill = data.GetProperty("skills").EnumerateArray().First(s => s.GetProperty("name").GetString() == "github");
        Assert.True(skill.GetProperty("installed").GetBoolean());
    }

    [Fact]
    public async Task ChannelTypes_Query_Returns_Logo()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ channelTypes { type displayName logo } }");
        var types = data.GetProperty("channelTypes");
        Assert.True(types.GetArrayLength() >= 6);
        var slack = types.EnumerateArray().First(t => t.GetProperty("type").GetString() == "slack");
        var logo = slack.GetProperty("logo").GetString();
        Assert.NotNull(logo);
        Assert.StartsWith("<svg", logo);
    }
}

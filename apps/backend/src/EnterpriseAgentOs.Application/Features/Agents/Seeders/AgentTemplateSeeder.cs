namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Seeds the 10 built-in agent templates surfaced in the Quickstart wizard.
/// Upserts by Name; built-in templates have IsBuiltin=true and OwnerId=null.
/// </summary>
public static class AgentTemplateSeeder
{
    private sealed record Seed(string Name, string Description, string[] Integrations, string[] Channels, string Prompt);

    private static readonly Seed[] Builtins =
    {
        new("Blank agent", "A blank starting point.", Array.Empty<string>(), Array.Empty<string>(), ""),
        new("Deep researcher", "Multi-step web research with citations.",
            new[] { "browser" }, Array.Empty<string>(),
            "You are a research assistant. Conduct thorough web research, synthesize findings, and present them with source citations."),
        new("Support agent", "Answers questions from docs, escalates via Slack.",
            new[] { "notion" }, new[] { "slack" },
            "You are a customer support agent. Answer questions using the knowledge base in Notion. Escalate to #support-escalation on Slack when needed."),
        new("Incident commander", "Triages alerts, creates tickets, runs war room.",
            new[] { "linear", "browser" }, new[] { "slack" },
            "You are an incident commander. Triage alerts, create Linear issues, and coordinate the response in #incidents on Slack."),
        new("Code reviewer", "Reviews PRs for bugs and security.",
            new[] { "github" }, Array.Empty<string>(),
            "Review pull request diffs for bugs, security vulnerabilities, and style issues. Leave constructive comments."),
        new("Feedback miner", "Clusters feedback into themes.",
            new[] { "notion" }, new[] { "slack" },
            "Collect feedback from Slack and Notion, cluster into themes, and draft actionable tasks."),
        new("Sprint retro", "Writes the retro doc from Linear.",
            new[] { "linear", "notion" }, Array.Empty<string>(),
            "Pull completed issues from the latest Linear sprint, identify patterns, and write a retro summary in Notion."),
        new("Compliance monitor", "Flags regulatory risks.",
            new[] { "browser", "notion" }, new[] { "slack" },
            "Search for regulatory updates, cross-reference against internal policies, and flag risks to #compliance."),
        new("Sales assistant", "Enriches leads and drafts outreach.",
            new[] { "hubspot", "browser" }, Array.Empty<string>(),
            "Research leads, draft personalized outreach emails, and log activity in HubSpot."),
        new("Data analyst", "Answers questions from web data.",
            new[] { "browser" }, Array.Empty<string>(),
            "Search for data sources, extract structured information, and present clear answers."),
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var repo = services.GetRequiredService<IAgentTemplateRepository>();
        var logger = services.GetRequiredService<ILogger<IAgentTemplateService>>();
        foreach (var s in Builtins)
        {
            await repo.UpsertAsync(new AgentTemplateRecord
            {
                Name = s.Name,
                Description = s.Description,
                Prompt = s.Prompt,
                IntegrationsJson = JsonSerializer.Serialize(s.Integrations),
                ChannelsJson = JsonSerializer.Serialize(s.Channels),
                IsBuiltin = true,
                OwnerId = null,
            }, ct);
        }
        logger.LogInformation("Seeded {Count} built-in agent templates", Builtins.Length);
    }
}

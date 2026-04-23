namespace EnterpriseAgentOs.Application.Features.Providers;

public static class ProviderSeeder
{
    public static async Task SeedAsync(IProviderRepository repo)
    {
        var existing = await repo.ListAsync();
        if (existing.Count > 0)
            return;

        var seed = new[]
        {
            new ProviderRecord { Name = "openai", DisplayName = "OpenAI" },
            new ProviderRecord { Name = "anthropic", DisplayName = "Anthropic" },
            new ProviderRecord { Name = "google", DisplayName = "Google Gemini" },
            new ProviderRecord { Name = "xai", DisplayName = "xAI Grok" },
        };

        foreach (var record in seed)
            await repo.SaveAsync(record);
    }
}

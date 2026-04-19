namespace EnterpriseAgentOs.Application.Services.Providers;

public static class ProviderSeeder
{
    public static async Task SeedAsync(EaosDbContext db)
    {
        if (await db.Providers.AnyAsync())
            return;

        var seed = new[]
        {
            new ProviderRecord { Name = "openai", DisplayName = "OpenAI" },
            new ProviderRecord { Name = "anthropic", DisplayName = "Anthropic" },
            new ProviderRecord { Name = "google", DisplayName = "Google Gemini" },
            new ProviderRecord { Name = "xai", DisplayName = "xAI Grok" },
        };

        await db.Providers.AddRangeAsync(seed);
        await db.SaveChangesAsync();
    }
}

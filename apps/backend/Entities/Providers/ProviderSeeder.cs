namespace EnterpriseAgentOs.Api.Entities.Providers;

public static class ProviderSeeder
{
    public static async Task SeedAsync(EnterpriseAgentOs.Api.Database.EaosDbContext db)
    {
        if (await db.Providers.AnyAsync())
            return;

        var seed = new[]
        {
            new EnterpriseAgentOs.Api.Database.Models.ProviderRecord { Name = "openai", DisplayName = "OpenAI" },
            new EnterpriseAgentOs.Api.Database.Models.ProviderRecord { Name = "anthropic", DisplayName = "Anthropic" },
            new EnterpriseAgentOs.Api.Database.Models.ProviderRecord { Name = "google", DisplayName = "Google Gemini" },
            new EnterpriseAgentOs.Api.Database.Models.ProviderRecord { Name = "xai", DisplayName = "xAI Grok" },
        };

        await db.Providers.AddRangeAsync(seed);
        await db.SaveChangesAsync();
    }
}

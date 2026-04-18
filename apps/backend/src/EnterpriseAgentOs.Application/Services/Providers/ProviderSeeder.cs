namespace EnterpriseAgentOs.Application.Services.Providers;

public static class ProviderSeeder
{
    public static async Task SeedAsync(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        if (await db.Providers.AnyAsync())
            return;

        var seed = new[]
        {
            new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "openai", DisplayName = "OpenAI" },
            new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "anthropic", DisplayName = "Anthropic" },
            new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "google", DisplayName = "Google Gemini" },
            new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "xai", DisplayName = "xAI Grok" },
        };

        await db.Providers.AddRangeAsync(seed);
        await db.SaveChangesAsync();
    }
}

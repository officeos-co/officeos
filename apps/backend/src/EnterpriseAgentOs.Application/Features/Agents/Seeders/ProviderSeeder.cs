namespace EnterpriseAgentOs.Application.Features.Agents;

public static class ProviderSeeder
{
    public static async Task SeedAsync(IProviderRepository repo)
    {
        var existing = await repo.ListAsync();
        if (existing.Count > 0)
            return;

        foreach (var def in ProviderRegistry.SeedableProviders)
            await repo.SaveAsync(new ProviderRecord { Name = def.Slug, DisplayName = def.DisplayName });
    }
}

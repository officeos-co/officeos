namespace EnterpriseAgentOs.Application.Features.Skills;

/// <summary>
/// Seeds builtin skill manifests from the skill-runtime on every startup.
/// Always upserts — populates new/updated columns even if skills already exist.
/// </summary>
public static class SkillSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SkillSeeder");
        var config = services.GetRequiredService<SkillRuntimeConfig>();
        var catalog = services.GetRequiredService<ISkillCatalogRepository>();

        IReadOnlyList<RuntimeManifest> manifests;
        try
        {
            using var http = new HttpClient();
            var url = config.Url.TrimEnd('/') + "/manifests";
            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Skill-runtime returned HTTP {StatusCode} — skipping seed", (int)resp.StatusCode);
                return;
            }
            var text = await resp.Content.ReadAsStringAsync();
            manifests = JsonSerializer.Deserialize<List<RuntimeManifest>>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<RuntimeManifest>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not reach skill-runtime — builtin skills will be seeded on next startup");
            return;
        }

        if (manifests.Count == 0)
        {
            logger.LogWarning("Skill-runtime returned 0 manifests — skipping seed");
            return;
        }

        var systemSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "browser" };
        var seeded = 0;
        var updated = 0;

        foreach (var manifest in manifests)
        {
            var name = manifest.Name.Trim().ToLowerInvariant();
            var existing = await catalog.GetByNameAsync(name);

            if (existing is null)
            {
                var record = new SkillRecord
                {
                    Name = name,
                    Title = manifest.Title,
                    Description = manifest.Description,
                    Doc = manifest.Doc,
                    Source = SkillSource.Builtin,
                    IsSystem = systemSkills.Contains(name),
                    Status = SkillStatus.Active,
                };
                record.ApplyManifest(manifest);
                await catalog.UpsertAsync(record);
                seeded++;
            }
            else if (existing.Source == SkillSource.Builtin)
            {
                existing.Title = manifest.Title;
                existing.Description = manifest.Description;
                existing.Doc = manifest.Doc;
                existing.IsSystem = systemSkills.Contains(name);
                existing.UpdatedAt = DateTime.UtcNow;
                existing.ApplyManifest(manifest);
                await catalog.UpsertAsync(existing);
                updated++;
            }
        }

        logger.LogInformation("Skill seeding complete — {Seeded} new, {Updated} updated, {Total} total",
            seeded, updated, manifests.Count);
    }
}

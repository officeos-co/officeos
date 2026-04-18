namespace EnterpriseAgentOs.Application.Services.Skills;

/// <summary>
/// Fallback skill seeder — only runs if the database has zero builtin skills.
/// Primary seeding is now handled by CI via POST /api/internal/seed-manifests.
/// This fallback exists for fresh deployments or database resets.
/// </summary>
public static class SkillSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SkillSeeder");
        var db = services.GetRequiredService<EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext>();

        var builtinCount = await db.Skills.CountAsync(s => s.Source == "builtin");
        if (builtinCount > 0)
        {
            logger.LogInformation("Found {Count} builtin skills in database — skipping runtime fallback", builtinCount);
            return;
        }

        logger.LogWarning("No builtin skills in database — falling back to runtime manifest fetch (this should only happen on first deployment)");

        var config = services.GetRequiredService<EnterpriseAgentOs.Infrastructure.Configuration.SkillRuntimeConfig>();
        IReadOnlyList<RuntimeManifest> manifests;
        try
        {
            using var http = new HttpClient();
            var url = config.Url.TrimEnd('/') + "/manifests";
            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Fallback: skill-runtime returned HTTP {StatusCode} — skipping seed", (int)resp.StatusCode);
                return;
            }
            var text = await resp.Content.ReadAsStringAsync();
            manifests = System.Text.Json.JsonSerializer.Deserialize<List<RuntimeManifest>>(text,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<RuntimeManifest>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallback: could not reach skill-runtime — builtin skills will be seeded by CI or on next startup");
            return;
        }

        if (manifests.Count == 0)
        {
            logger.LogWarning("Fallback: skill-runtime returned 0 manifests — skipping seed");
            return;
        }

        var systemSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "browser" };
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };

        foreach (var manifest in manifests)
        {
            var name = manifest.Name.Trim().ToLowerInvariant();
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, jsonOptions);
            db.Skills.Add(new EnterpriseAgentOs.Domain.Models.SkillRecord
            {
                Name = name,
                Title = manifest.Title,
                Description = manifest.Description,
                Doc = manifest.Doc,
                Source = "builtin",
                ManifestJson = manifestJson,
                IsSystem = systemSkills.Contains(name),
                Status = "active",
            });
            logger.LogInformation("Seeded builtin skill (fallback): {SkillName}", name);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Fallback skill seeding complete — {Count} builtin skills", manifests.Count);
    }
}

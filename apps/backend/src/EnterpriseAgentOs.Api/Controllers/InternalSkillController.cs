namespace EnterpriseAgentOs.Api.Controllers;

[ApiController]
[Route("api/internal")]
public sealed class InternalSkillController : ControllerBase
{
    private readonly ISkillCatalogRepository _catalog;
    private readonly ILogger<InternalSkillController> _logger;

    public InternalSkillController(
        ISkillCatalogRepository catalog,
        ILogger<InternalSkillController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    /// <summary>
    /// Seed builtin skill manifests from CI pipeline.
    /// Replaces the old pattern of fetching manifests from the skill-runtime at startup.
    /// </summary>
    [HttpPost("seed-manifests")]
    public async Task<IActionResult> SeedManifests([FromBody] List<RuntimeManifest> manifests, CancellationToken ct)
    {
        if (manifests.Count == 0)
        {
            return BadRequest(new { error = "No manifests provided" });
        }

        var systemSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "browser" };
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var seeded = 0;
        var updated = 0;

        foreach (var manifest in manifests)
        {
            var name = manifest.Name.Trim().ToLowerInvariant();
            var manifestJson = JsonSerializer.Serialize(manifest, jsonOptions);
            var existing = await _catalog.GetByNameAsync(name, ct);

            if (existing is null)
            {
                await _catalog.UpsertAsync(new SkillRecord
                {
                    Name = name,
                    Title = manifest.Title,
                    Description = manifest.Description,
                    Doc = manifest.Doc,
                    Source = "builtin",
                    ManifestJson = manifestJson,
                    IsSystem = systemSkills.Contains(name),
                    Status = "active",
                }, ct);
                seeded++;
                _logger.LogInformation("Seeded builtin skill: {SkillName}", name);
            }
            else if (existing.Source == "builtin")
            {
                existing.Title = manifest.Title;
                existing.Description = manifest.Description;
                existing.Doc = manifest.Doc;
                existing.ManifestJson = manifestJson;
                existing.IsSystem = systemSkills.Contains(name);
                existing.UpdatedAt = DateTime.UtcNow;
                await _catalog.UpsertAsync(existing, ct);
                updated++;
                _logger.LogDebug("Updated builtin skill manifest: {SkillName}", name);
            }
        }

        _logger.LogInformation("Manifest seeding complete — {Seeded} new, {Updated} updated, {Total} total",
            seeded, updated, manifests.Count);

        return Ok(new { seeded, updated, total = manifests.Count });
    }
}

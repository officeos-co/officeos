namespace EnterpriseAgentOs.Api.Features.Agents;

/// <summary>
/// Non-dashboard skill endpoints. Dashboard skill catalog/install/credentials
/// have moved to GraphQL (Entities/Skills/GraphQL/). This controller only
/// serves the skill bundle download used by skill-runtime for on-demand loading.
/// </summary>
[ApiController]
[Route("api/skills")]
public sealed class SkillController : ControllerBase
{
    private readonly ISkillCatalogRepository _skillCatalogRepository;
    private readonly IAmazonS3? _amazonS3;
    private readonly SkillStorageConfig _skillStorageConfig;

    public SkillController(
        ISkillCatalogRepository catalog,
        SkillStorageConfig storage,
        IAmazonS3? s3 = null)
    {
        _skillCatalogRepository = catalog;
        _amazonS3 = s3;
        _skillStorageConfig = storage;
    }

    // ---------- bundle download (for skill-runtime on-demand loading) ----------

    [HttpGet("{name}/bundle")]
    public async Task<IActionResult> GetBundle(string name, CancellationToken ct)
    {
        if (_amazonS3 is null)
            return StatusCode(503, new { error = "Object storage not configured" });

        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null || string.IsNullOrEmpty(skill.BundleS3Key))
            return NotFound(new { error = "No bundle available for this skill" });

        try
        {
            var response = await _amazonS3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _skillStorageConfig.Bucket,
                Key = skill.BundleS3Key,
            }, ct);

            return File(response.ResponseStream, "application/javascript", $"{name}.js");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new { error = "Bundle not found in storage" });
        }
    }
}

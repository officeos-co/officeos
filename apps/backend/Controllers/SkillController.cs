namespace EnterpriseAgentOs.Api.Controllers;

/// <summary>
/// Non-dashboard skill endpoints. Dashboard skill catalog/install/credentials
/// have moved to GraphQL (Entities/Skills/GraphQL/). This controller only
/// serves the skill bundle download used by skill-runtime for on-demand loading.
/// </summary>
[ApiController]
[Route("api/skills")]
public sealed class SkillController : ControllerBase
{
    private readonly ISkillCatalogRepository _catalog;
    private readonly IAmazonS3 _s3;
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.SkillStorageConfig _storage;

    public SkillController(
        ISkillCatalogRepository catalog,
        IAmazonS3 s3,
        EnterpriseAgentOs.Infrastructure.Configuration.SkillStorageConfig storage)
    {
        _catalog = catalog;
        _s3 = s3;
        _storage = storage;
    }

    // ---------- bundle download (for skill-runtime on-demand loading) ----------

    [HttpGet("{name}/bundle")]
    public async Task<IActionResult> GetBundle(string name, CancellationToken ct)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null || string.IsNullOrEmpty(skill.BundleS3Key))
            return NotFound(new { error = "No bundle available for this skill" });

        try
        {
            var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _storage.Bucket,
                Key = skill.BundleS3Key,
            }, ct);

            return File(response.ResponseStream, "application/javascript", $"{name}.js");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = "Bundle not found in storage" });
        }
    }
}

using System.IO.Compression;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using EnterpriseAgentOs.Api.Entities.Skills;

namespace EnterpriseAgentOs.Api.Entities.CustomSkills;

[ApiController]
[Route("api/custom-skills")]
public sealed class CustomSkillsController : ControllerBase
{
    private readonly ICustomSkillRepository _repo;
    private readonly ISkillCatalogRepository _catalog;
    private readonly SkillRuntimeClient _runtime;
    private readonly IAmazonS3 _s3;
    private readonly SkillStorageConfig _storage;

    private readonly ILogger<CustomSkillsController> _logger;

    public CustomSkillsController(
        ICustomSkillRepository repo,
        ISkillCatalogRepository catalog,
        SkillRuntimeClient runtime,
        IAmazonS3 s3,
        SkillStorageConfig storage,
        ILogger<CustomSkillsController> logger)
    {
        _repo = repo;
        _catalog = catalog;
        _runtime = runtime;
        _s3 = s3;
        _storage = storage;
        _logger = logger;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var skills = await _repo.ListAsync(ct);
        return Ok(skills.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            source = s.Source,
            gitHubRepoUrl = s.GitHubRepoUrl,
            buildStatus = s.BuildStatus,
            buildError = s.BuildError,
            createdAt = s.CreatedAt,
            lastSyncAt = s.LastSyncAt,
        }));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });
        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be a .zip archive" });

        // Read zip into memory (needed for both S3 upload and extraction)
        using var memStream = new MemoryStream();
        await file.CopyToAsync(memStream, ct);
        memStream.Position = 0;

        // Validate zip contents
        using var archive = new ZipArchive(memStream, ZipArchiveMode.Read, leaveOpen: true);
        var hasSkillTs = archive.Entries.Any(e =>
            e.FullName.EndsWith("skill.ts", StringComparison.OrdinalIgnoreCase));
        if (!hasSkillTs)
            return BadRequest(new { error = "Zip must contain a skill.ts file" });

        // Read all files into memory for sending to skill-runtime
        var files = new List<object>();
        string? skillName = null;

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0) continue;
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync(ct);
            files.Add(new { path = entry.FullName, content });

            if (entry.FullName.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var pkg = JsonSerializer.Deserialize<JsonElement>(content);
                    if (pkg.TryGetProperty("name", out var n))
                        skillName = n.GetString()?.Replace("@", "").Replace("/", "-");
                }
                catch { }
            }
        }

        skillName ??= System.IO.Path.GetFileNameWithoutExtension(file.FileName).ToLowerInvariant();

        // Store zip in MinIO (skills/{name}.zip)
        memStream.Position = 0;
        var s3Key = $"{skillName}/{skillName}.zip";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _storage.Bucket,
            Key = s3Key,
            InputStream = memStream,
            ContentType = "application/zip",
        }, ct);

        var record = new CustomSkillRecord
        {
            OwnerId = CurrentUser.Id,
            Name = skillName,
            Source = "upload",
            BundlePath = $"s3://{_storage.Bucket}/{s3Key}",
            BuildStatus = "building",
        };
        await _repo.CreateAsync(record, ct);
        _logger.LogInformation("Custom skill {SkillName} uploaded to S3, building", skillName);

        // Send to skill-runtime for building
        try
        {
            var buildResult = await _runtime.BuildAsync(skillName, files, ct);
            record.BuildStatus = "ready";
            record.LastSyncAt = DateTime.UtcNow;
            _logger.LogInformation("Custom skill {SkillName} built successfully", skillName);

            // Create SkillRecord in the catalog so the skill appears in GET /api/skills
            await CreateSkillRecordFromBuildResult(skillName, s3Key, buildResult, ct);
        }
        catch (Exception ex)
        {
            record.BuildStatus = "failed";
            record.BuildError = ex.Message;
            _logger.LogError(ex, "Custom skill {SkillName} build failed", skillName);
        }

        await _repo.UpdateAsync(record, ct);

        return Ok(new
        {
            id = record.Id,
            name = record.Name,
            buildStatus = record.BuildStatus,
            buildError = record.BuildError,
        });
    }

    [HttpPost("github")]
    public async Task<IActionResult> ConnectGitHub([FromBody] ConnectGitHubRequest body, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(body.RepoUrl))
            return BadRequest(new { error = "repoUrl is required" });

        var skillName = body.RepoUrl.Split('/').LastOrDefault()?.ToLowerInvariant() ?? "custom-skill";

        var record = new CustomSkillRecord
        {
            OwnerId = CurrentUser.Id,
            Name = skillName,
            Source = "github",
            GitHubRepoUrl = body.RepoUrl,
            GitHubBranch = body.Branch ?? "main",
            BuildStatus = "pending",
        };
        await _repo.CreateAsync(record, ct);
        _logger.LogInformation("Custom skill {SkillName} connected to GitHub repo {RepoUrl}",
            skillName, body.RepoUrl);

        return Ok(new
        {
            id = record.Id,
            name = record.Name,
            source = record.Source,
            buildStatus = record.BuildStatus,
        });
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _repo.GetByIdAsync(id, ct);
        if (record is null || record.OwnerId != CurrentUser.Id)
            return NotFound();

        record.BuildStatus = "building";
        record.BuildError = null;
        await _repo.UpdateAsync(record, ct);

        // TODO: Clone repo and send to skill-runtime for building
        record.BuildStatus = "pending";
        record.LastSyncAt = DateTime.UtcNow;
        await _repo.UpdateAsync(record, ct);

        return Ok(new { id = record.Id, buildStatus = record.BuildStatus });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _repo.GetByIdAsync(id, ct);
        if (record is null || record.OwnerId != CurrentUser.Id)
            return NotFound();

        // Delete from S3 if exists
        if (!string.IsNullOrEmpty(record.BundlePath))
        {
            var s3Key = $"{record.Name}/{record.Name}.zip";
            try
            {
                await _s3.DeleteObjectAsync(_storage.Bucket, s3Key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete S3 object {S3Key} for custom skill {SkillName}",
                    s3Key, record.Name);
            }
        }

        await _repo.DeleteAsync(id, ct);
        await _catalog.DeleteByNameAsync(record.Name, ct);
        _logger.LogInformation("Custom skill {SkillId} ({SkillName}) deleted", id, record.Name);
        return NoContent();
    }

    private async Task CreateSkillRecordFromBuildResult(
        string skillName, string s3Key, System.Text.Json.JsonElement buildResult, CancellationToken ct)
    {
        try
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };

            // Build response is { ok: true, manifest: { ... } }
            if (!buildResult.TryGetProperty("manifest", out var manifestElement))
            {
                _logger.LogWarning("Build result for {SkillName} did not contain a manifest", skillName);
                return;
            }

            var manifest = System.Text.Json.JsonSerializer.Deserialize<RuntimeManifest>(
                manifestElement.GetRawText(), jsonOptions);
            if (manifest is null)
            {
                _logger.LogWarning("Could not deserialize manifest for uploaded skill {SkillName}", skillName);
                return;
            }

            await _catalog.UpsertAsync(new SkillRecord
            {
                Name = skillName,
                Title = manifest.Title,
                Description = manifest.Description,
                Emoji = manifest.Emoji,
                Doc = manifest.Doc,
                Source = "upload",
                ManifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, jsonOptions),
                BundleS3Key = s3Key,
                Status = "active",
                OwnerId = CurrentUser?.Id,
            }, ct);
            _logger.LogInformation("Created SkillRecord for uploaded skill {SkillName}", skillName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create SkillRecord for uploaded skill {SkillName}", skillName);
        }
    }
}

public sealed record ConnectGitHubRequest(string RepoUrl, string? Branch = null);

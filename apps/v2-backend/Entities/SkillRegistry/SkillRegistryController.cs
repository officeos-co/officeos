using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.SkillRegistry;

[ApiController]
[Route("api/skill-registry")]
public sealed class SkillRegistryController : ControllerBase
{
    private readonly ISkillRegistryRepository _repo;
    private readonly SkillRuntimeClient _runtime;
    private readonly ILogger<SkillRegistryController> _logger;

    public SkillRegistryController(
        ISkillRegistryRepository repo,
        SkillRuntimeClient runtime,
        ILogger<SkillRegistryController> logger)
    {
        _repo = repo;
        _runtime = runtime;
        _logger = logger;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    /// <summary>
    /// List all skills in the registry.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var records = await _repo.ListAsync(ct);
        return Ok(records.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            version = r.Version,
            npmPackage = r.NpmPackage,
            bundleUrl = r.BundleUrl,
            status = r.Status,
            publishedById = r.PublishedById,
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt,
        }));
    }

    /// <summary>
    /// Publish a new skill to the registry.
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishRequest body, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { error = "name is required" });

        var existing = await _repo.GetByNameAsync(body.Name, ct);
        if (existing is not null)
            return Conflict(new { error = $"Skill '{body.Name}' already exists in the registry" });

        var record = new SkillRegistryRecord
        {
            Name = body.Name.Trim().ToLowerInvariant(),
            Version = body.Version ?? "1.0.0",
            NpmPackage = body.NpmPackage,
            BundleUrl = body.BundleUrl,
            ManifestJson = body.ManifestJson is not null
                ? JsonSerializer.Serialize(body.ManifestJson)
                : null,
            Status = "active",
            PublishedById = CurrentUser.Id,
        };
        await _repo.CreateAsync(record, ct);
        _logger.LogInformation("Skill {SkillName} v{Version} published to registry by {UserId}",
            record.Name, record.Version, CurrentUser.Id);

        // Notify skill-runtime to install if bundle is available
        if (!string.IsNullOrEmpty(body.BundleUrl) || !string.IsNullOrEmpty(body.NpmPackage))
        {
            try
            {
                await _runtime.InstallFromRegistryAsync(record.Name, body.NpmPackage, body.BundleUrl, ct);
                _logger.LogInformation("Skill {SkillName} installed in runtime from registry", record.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to install skill {SkillName} in runtime during publish", record.Name);
            }
        }

        return Ok(new
        {
            id = record.Id,
            name = record.Name,
            version = record.Version,
            status = record.Status,
        });
    }

    /// <summary>
    /// Update a skill's status (active/disabled).
    /// </summary>
    [HttpPatch("{name}")]
    public async Task<IActionResult> UpdateStatus(string name, [FromBody] UpdateStatusRequest body, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _repo.GetByNameAsync(name, ct);
        if (record is null)
            return NotFound(new { error = $"Skill '{name}' not found in registry" });

        if (!string.IsNullOrEmpty(body.Status))
        {
            if (body.Status is not ("active" or "disabled"))
                return BadRequest(new { error = "Status must be 'active' or 'disabled'" });
            record.Status = body.Status;
        }

        if (!string.IsNullOrEmpty(body.Version))
            record.Version = body.Version;

        await _repo.UpdateAsync(record, ct);
        _logger.LogInformation("Skill {SkillName} updated in registry: status={Status}", name, record.Status);

        return Ok(new
        {
            id = record.Id,
            name = record.Name,
            version = record.Version,
            status = record.Status,
            updatedAt = record.UpdatedAt,
        });
    }

    /// <summary>
    /// Remove a skill from the registry.
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _repo.GetByNameAsync(name, ct);
        if (record is null)
            return NotFound(new { error = $"Skill '{name}' not found in registry" });

        // Uninstall from runtime
        try
        {
            await _runtime.UninstallFromRegistryAsync(name, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to uninstall skill {SkillName} from runtime during delete", name);
        }

        await _repo.DeleteByNameAsync(name, ct);
        _logger.LogInformation("Skill {SkillName} removed from registry by {UserId}", name, CurrentUser.Id);
        return NoContent();
    }
}

public sealed record PublishRequest(
    string Name,
    string? Version = null,
    string? NpmPackage = null,
    string? BundleUrl = null,
    object? ManifestJson = null
);

public sealed record UpdateStatusRequest(
    string? Status = null,
    string? Version = null
);

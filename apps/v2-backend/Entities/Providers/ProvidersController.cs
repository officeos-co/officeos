
namespace EnterpriseAgentOs.Api.Entities.Providers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    /// <summary>
    /// Providers whose keys are held by the platform (never BYOK).
    /// </summary>
    private static readonly HashSet<string> PlatformKeyProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "anthropic", "google", "xai",
    };

    private readonly IProviderService _service;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(IProviderService service, ILogger<ProvidersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderDto>>> List(CancellationToken ct)
    {
        var providers = await _service.ListAsync(ct);

        // Platform-key providers are always treated as configured
        var result = providers.Select(p =>
            PlatformKeyProviders.Contains(p.Name)
                ? p with { Configured = true }
                : p
        ).ToList();

        return Ok(result);
    }

    [HttpPut("{name}")]
    public async Task<ActionResult<ProviderDto>> Configure(
        string name,
        [FromBody] ConfigureProviderRequest request,
        CancellationToken ct)
    {
        if (PlatformKeyProviders.Contains(name))
        {
            return BadRequest(new
            {
                error = "Only OpenAI supports custom API keys. Other providers use platform keys."
            });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _service.ConfigureAsync(name, request.ApiKey, ct);
        if (updated is null)
        {
            _logger.LogWarning("Configure provider {ProviderName}: not found", name);
            return NotFound();
        }
        _logger.LogInformation("Provider {ProviderName} configured", name);
        return Ok(updated);
    }

    [HttpGet("{name}/models")]
    public ActionResult<IReadOnlyList<string>> GetModels(string name)
    {
        var models = KnownModels.For(name.Trim().ToLowerInvariant());
        if (models.Count == 0)
        {
            return NotFound();
        }
        return Ok(models);
    }

    [HttpDelete("{name}/key")]
    public async Task<IActionResult> Clear(string name, CancellationToken ct)
    {
        if (PlatformKeyProviders.Contains(name))
        {
            return BadRequest(new
            {
                error = "Only OpenAI supports custom API keys. Other providers use platform keys."
            });
        }

        var cleared = await _service.ClearAsync(name, ct);
        if (!cleared)
        {
            return NotFound();
        }
        return NoContent();
    }
}

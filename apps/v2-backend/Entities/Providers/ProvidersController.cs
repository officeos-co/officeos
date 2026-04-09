
namespace EnterpriseAgentOs.Api.Entities.Providers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly IProviderService _service;

    public ProvidersController(IProviderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderDto>>> List(CancellationToken ct)
    {
        var providers = await _service.ListAsync(ct);
        return Ok(providers);
    }

    [HttpPut("{name}")]
    public async Task<ActionResult<ProviderDto>> Configure(
        string name,
        [FromBody] ConfigureProviderRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _service.ConfigureAsync(name, request.ApiKey, ct);
        if (updated is null)
        {
            return NotFound();
        }
        return Ok(updated);
    }

    [HttpDelete("{name}/key")]
    public async Task<IActionResult> Clear(string name, CancellationToken ct)
    {
        var cleared = await _service.ClearAsync(name, ct);
        if (!cleared)
        {
            return NotFound();
        }
        return NoContent();
    }
}

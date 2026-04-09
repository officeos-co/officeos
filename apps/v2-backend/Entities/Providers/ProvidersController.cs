using EnterpriseAgentOs.Api.Entities.Providers.Interfaces;
using EnterpriseAgentOs.Api.Entities.Providers.Models;
using Microsoft.AspNetCore.Mvc;

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
}

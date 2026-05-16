using OffceOs.Application.Features.ControlPlane;
using OffceOs.Domain.Features.Management;
namespace OffceOs.Api.Features.ControlPlane;

[ApiController]
[Route("api/v1/resources")]
public sealed class ControlPlaneResourceCatalogController : ControllerBase
{
    [HttpGet]
    public IActionResult ListResources(
        [FromServices] IControlPlaneResourceCatalogService catalog)
    {
        if (HttpContext.Items["User"] is not UserRecord)
            return Unauthorized(new { error = "Unauthenticated." });

        return Ok(catalog.List());
    }
}

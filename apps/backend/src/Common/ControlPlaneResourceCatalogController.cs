namespace OffceOs.Api.Common;

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

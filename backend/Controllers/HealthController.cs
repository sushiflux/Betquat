using Microsoft.AspNetCore.Mvc;

namespace BetQuote.Api.Controllers;

/// <summary>
/// Minimaler Platzhalter-Endpoint fuers Grundgeruest. Bestaetigt, dass Backend + Reverse Proxy
/// erreichbar sind. Die eigentliche Fachlogik (Spiele, Vorhersagen, ...) folgt in separaten
/// Schritten, siehe PREDICTION_FACTORS.md / APP_STRUCTURE.md.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "BetQuote.Api" });
}

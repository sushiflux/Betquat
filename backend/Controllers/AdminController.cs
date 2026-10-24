using BetQuote.Api.Services.FootballDataOrg;
using BetQuote.Api.Services.OpenLigaDb;
using BetQuote.Api.Services.Prediction;
using BetQuote.Api.Services.RadioLink;
using BetQuote.Api.Services.Trefferquote;
using Microsoft.AspNetCore.Mvc;

namespace BetQuote.Api.Controllers;

/// <summary>
/// Manuelle Trigger fuer die sonst zeitgesteuerten Batch-/Hintergrundjobs (Import, Vorhersage-
/// Berechnung, Trefferquote-Aktualisierung) - fuer diesen Einzelnutzer ohne Auth (siehe
/// CLAUDE.md-Projektvorgabe) bewusst ohne Schutz zugaenglich, dient primaer dem manuellen
/// Anstossen/Debuggen ausserhalb der reglaeren Zeitplaene (z.B. nach einem Deploy sofort Daten
/// sehen wollen statt auf das naechste Scheduler-Intervall zu warten).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController(
    IOpenLigaDbImportService importService,
    IFootballDataOrgImportService footballDataOrgImportService,
    IPredictionService predictionService,
    ITrefferquoteService trefferquoteService,
    IRadioLinkImportService radioLinkImportService) : ControllerBase
{
    [HttpPost("import/alle")]
    public async Task<ActionResult<ImportSummary>> TriggerVollimport(CancellationToken ct)
    {
        var summary = await importService.ImportAllAsync(ct);
        return Ok(summary);
    }

    [HttpPost("import/aktuell")]
    public async Task<ActionResult<ImportSummary>> TriggerAktuellenImport(CancellationToken ct)
    {
        var summary = await importService.ImportCurrentSeasonsAsync(ct);
        return Ok(summary);
    }

    /// <summary>Manueller Trigger fuer den La-Liga-Import (football-data.org) - liefert eine ImportSummary mit Fehlermeldung statt Daten, solange FOOTBALL_DATA_ORG_API_KEY nicht gesetzt ist (siehe FootballDataOrgClient).</summary>
    [HttpPost("import/laliga/alle")]
    public async Task<ActionResult<ImportSummary>> TriggerLaLigaVollimport(CancellationToken ct)
    {
        var summary = await footballDataOrgImportService.ImportAllAsync(ct);
        return Ok(summary);
    }

    [HttpPost("import/laliga/aktuell")]
    public async Task<ActionResult<ImportSummary>> TriggerLaLigaAktuellenImport(CancellationToken ct)
    {
        var summary = await footballDataOrgImportService.ImportCurrentAsync(ct);
        return Ok(summary);
    }

    [HttpPost("vorhersagen/berechnen")]
    public async Task<IActionResult> TriggerVorhersageBerechnung([FromQuery] int tageVoraus = 7, CancellationToken ct = default)
    {
        var anzahl = await predictionService.BerechneAusstehendeVorhersagenAsync(tageVoraus, ct);
        return Ok(new { anzahlBerechnet = anzahl });
    }

    [HttpPost("trefferquote/aktualisieren")]
    public async Task<IActionResult> TriggerTrefferquoteAktualisierung(CancellationToken ct)
    {
        var ausgewertet = await trefferquoteService.AuswerteAbgeschlosseneVorhersagenAsync(ct);
        var aktualisiert = await trefferquoteService.AktualisiereAbgeschlosseneSpieltageAsync(ct);
        return Ok(new { ausgewertet, spieltageAktualisiert = aktualisiert });
    }

    [HttpPost("radiolinks/aktualisieren")]
    public async Task<IActionResult> TriggerRadioLinkImport(CancellationToken ct)
    {
        var anzahl = await radioLinkImportService.ImportAktuelleRadioLinksAsync(ct);
        return Ok(new { anzahlZugeordnet = anzahl });
    }
}

using System.Text.Json;
using BetQuote.Api.Controllers.Dtos;
using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.Prediction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Controllers;

/// <summary>
/// Wochen-Feed und Spiel-Detail (APP_STRUCTURE.md Abschnitte 1 + 2). Kein Auth (privates Tool,
/// siehe CLAUDE.md-Projektvorgabe).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SpieleController(BetQuoteDbContext db, ITeamFormService formService) : ControllerBase
{
    /// <summary>
    /// GET /api/spiele/woche?jahr=2026&amp;kw=12[&amp;wettbewerbId=1&amp;wettbewerbId=2] - Spiele
    /// einer Kalenderwoche, nach Wochentag gruppiert. Optionaler wettbewerbId-Filter (mehrfach
    /// wiederholbarer Query-Parameter, ASP.NET Core bindet das automatisch als Array): das
    /// Frontend zeigt den Wochen-Feed inzwischen ueber eine Land/Liga-Tab-Navigation gefiltert an
    /// (Deutschland: 1./2. Bundesliga zusammen = zwei Ids in einem Request, Spanien: nur La Liga =
    /// eine Id - siehe APP_STRUCTURE.md), nicht mehr zwangsweise alle Wettbewerbe gemischt. Ohne
    /// den Parameter bleibt das bisherige Verhalten (alle Wettbewerbe gemischt) unveraendert
    /// erhalten - rueckwaertskompatibel fuer bestehende Aufrufer.
    /// </summary>
    [HttpGet("woche")]
    public async Task<ActionResult<WochenFeedDto>> GetWoche([FromQuery] int jahr, [FromQuery] int kw, [FromQuery] int[]? wettbewerbId, CancellationToken ct)
    {
        if (kw is < 1 or > 53)
        {
            return BadRequest(new { error = "kw muss zwischen 1 und 53 liegen." });
        }

        var query = db.Spiele
            .AsNoTracking()
            .Include(s => s.Wettbewerb)
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .Include(s => s.RadioLink)
            .Include(s => s.Vorhersagen.Where(v => v.IstAktuell))
            .Where(s => s.KwJahr == jahr && s.KwNummer == kw);

        if (wettbewerbId is { Length: > 0 })
        {
            query = query.Where(s => wettbewerbId.Contains(s.WettbewerbId));
        }

        var spiele = await query
            .OrderBy(s => s.Anstoss)
            .ToListAsync(ct);

        var tage = spiele
            .GroupBy(s => DateOnly.FromDateTime(s.Anstoss.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new WochenFeedTagDto(g.Key, [.. g.Select(s => s.ToListeDto())]))
            .ToList();

        return Ok(new WochenFeedDto(jahr, kw, tage));
    }

    /// <summary>GET /api/spiele/{id} - vollstaendiges Spiel-Detail inkl. Wahrscheinlichkeitsverteilung, Faktoren-Begruendung, Torschuetzen, H2H, Formkurve.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SpielDetailDto>> GetDetail(int id, CancellationToken ct)
    {
        var spiel = await db.Spiele
            .AsNoTracking()
            .Include(s => s.Wettbewerb)
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .Include(s => s.RadioLink)
            .Include(s => s.TorEvents)
            .Include(s => s.Vorhersagen.Where(v => v.IstAktuell))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (spiel is null)
        {
            return NotFound(new { error = $"Spiel mit Id {id} nicht gefunden." });
        }

        var h2h = await formService.GetHeadToHeadAsync(spiel.HeimTeamId, spiel.AuswaertsTeamId, spiel.Anstoss == default ? DateTimeOffset.UtcNow : spiel.Anstoss.AddSeconds(1), ct: ct);

        // Formkurve fuer die Detailanzeige (letzte 5 Spiele je Team, unabhaengig vom Vorhersage-Snapshot -
        // bei bereits gespielten Partien zeigt das bewusst den aktuellen Stand, nicht den Stand vor Anpfiff).
        var formHeim = await formService.GetFormAsync(spiel.HeimTeamId, spiel.WettbewerbId, spiel.SaisonId, DateTimeOffset.UtcNow, ct: ct);
        var formAuswaerts = await formService.GetFormAsync(spiel.AuswaertsTeamId, spiel.WettbewerbId, spiel.SaisonId, DateTimeOffset.UtcNow, ct: ct);

        var vorhersage = spiel.Vorhersagen.FirstOrDefault();

        var dto = new SpielDetailDto(
            spiel.Id,
            spiel.WettbewerbId,
            spiel.Wettbewerb.Name,
            spiel.SpieltagNummer,
            spiel.SpieltagName,
            spiel.Anstoss,
            spiel.HeimTeam.ToDto(),
            spiel.AuswaertsTeam.ToDto(),
            spiel.Status.ToStatusString(),
            spiel.LiveMinute,
            spiel.ToreHeim,
            spiel.ToreAuswaerts,
            spiel.HalbzeitToreHeim,
            spiel.HalbzeitToreAuswaerts,
            [.. spiel.TorEvents.OrderBy(t => t.Minute).Select(t => new TorEventDto(t.TeamId, t.SpielerNameFreitext, t.Minute, t.IstElfmeter, t.IstEigentor, t.ToreHeimNachEvent, t.ToreAuswaertsNachEvent))],
            vorhersage is null ? null : ToDetailDto(vorhersage),
            [.. h2h.OrderByDescending(s => s.Anstoss).Select(s => new HeadToHeadEintragDto(s.Anstoss, s.HeimTeamId == spiel.HeimTeamId ? spiel.HeimTeam.Name : spiel.AuswaertsTeam.Name, s.AuswaertsTeamId == spiel.AuswaertsTeamId ? spiel.AuswaertsTeam.Name : spiel.HeimTeam.Name, s.ToreHeim, s.ToreAuswaerts))],
            [.. formHeim.FormkurveLetzte5],
            [.. formAuswaerts.FormkurveLetzte5],
            spiel.RadioLink is null ? null : new RadioLinkDto(spiel.RadioLink.EinzelspielUrl, spiel.RadioLink.KonferenzUrl));

        return Ok(dto);
    }

    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static VorhersageDetailDto ToDetailDto(Vorhersage v)
    {
        var verteilung = JsonSerializer.Deserialize<List<ErgebnisWahrscheinlichkeitJsonDto>>(v.Ergebnisverteilung.RootElement.GetRawText(), CaseInsensitiveJsonOptions) ?? [];

        return new VorhersageDetailDto(
            v.TippToreHeim,
            v.TippToreAuswaerts,
            v.TippWahrscheinlichkeit,
            v.WahrscheinlichkeitHeimsieg,
            v.WahrscheinlichkeitRemis,
            v.WahrscheinlichkeitAuswaertssieg,
            v.WahrscheinlichkeitUeber25,
            v.WahrscheinlichkeitBeideTreffen,
            [.. verteilung.Select(e => new ErgebnisWahrscheinlichkeitDto(e.Heim, e.Auswaerts, e.Wahrscheinlichkeit))],
            JsonSerializer.Deserialize<object>(v.FaktorenSnapshot.RootElement.GetRawText())!,
            v.TrafExakt,
            v.TrafTendenz,
            v.BerechnetAm,
            v.IstHoheKonfidenz,
            v.KonfidenzScore);
    }

    private record ErgebnisWahrscheinlichkeitJsonDto(int Heim, int Auswaerts, decimal Wahrscheinlichkeit);
}

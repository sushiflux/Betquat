using BetQuote.Api.Controllers.Dtos;
using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Controllers;

/// <summary>Statistik-Widget "Wie gut lag ich richtig?" (APP_STRUCTURE.md Abschnitt 3) - Trefferquote pro Wettbewerb mit Drill-down Gesamt-&gt;Saison-&gt;Spieltag.</summary>
[ApiController]
[Route("api/[controller]")]
public class StatistikController(BetQuoteDbContext db) : ControllerBase
{
    /// <summary>GET /api/statistik/trefferquote - kompakte Uebersicht: Gesamt-Trefferquote je Wettbewerb plus Saison-Zeilen (fuer die verdichtete Widget-Ansicht, APP_STRUCTURE.md: "kompakte Kennzahlen-Ansicht").</summary>
    [HttpGet("trefferquote")]
    public async Task<ActionResult<List<StatistikWettbewerbDto>>> GetTrefferquoteUebersicht(CancellationToken ct)
    {
        var wettbewerbe = await db.Wettbewerbe.AsNoTracking().OrderBy(w => w.Sortierung).ToListAsync(ct);

        var eintraege = await db.TrefferquoteEintraege
            .AsNoTracking()
            .Include(t => t.Saison)
            .Where(t => t.Ebene == TrefferquoteEbene.Gesamt || t.Ebene == TrefferquoteEbene.Saison)
            .ToListAsync(ct);

        var ergebnis = wettbewerbe.Select(w =>
        {
            var gesamt = eintraege.FirstOrDefault(e => e.WettbewerbId == w.Id && e.Ebene == TrefferquoteEbene.Gesamt);
            var saisons = eintraege
                .Where(e => e.WettbewerbId == w.Id && e.Ebene == TrefferquoteEbene.Saison)
                .OrderByDescending(e => e.Saison?.Startjahr)
                .Select(ToZeileDto)
                .ToList();

            return new StatistikWettbewerbDto(w.Id, w.Name, gesamt is null ? null : ToZeileDto(gesamt), saisons);
        }).ToList();

        return Ok(ergebnis);
    }

    /// <summary>GET /api/statistik/trefferquote/{wettbewerbId}/saison/{saisonId} - Drill-down auf einzelne Spieltag-Zeilen einer Saison.</summary>
    [HttpGet("trefferquote/{wettbewerbId:int}/saison/{saisonId:int}")]
    public async Task<ActionResult<StatistikDrilldownDto>> GetSpieltagDrilldown(int wettbewerbId, int saisonId, CancellationToken ct)
    {
        var existiertSaison = await db.Saisons.AsNoTracking().AnyAsync(s => s.Id == saisonId && s.WettbewerbId == wettbewerbId, ct);
        if (!existiertSaison)
        {
            return NotFound(new { error = $"Saison {saisonId} fuer Wettbewerb {wettbewerbId} nicht gefunden." });
        }

        var eintraege = await db.TrefferquoteEintraege
            .AsNoTracking()
            .Include(t => t.Saison)
            .Where(t => t.WettbewerbId == wettbewerbId && t.SaisonId == saisonId && t.Ebene == TrefferquoteEbene.Spieltag)
            .OrderBy(t => t.SpieltagNummer)
            .ToListAsync(ct);

        var spieltage = eintraege.Select(ToZeileDto).ToList();

        return Ok(new StatistikDrilldownDto(wettbewerbId, saisonId, spieltage));
    }

    private static TrefferquoteZeileDto ToZeileDto(TrefferquoteEintrag e) => new(
        e.Ebene.ToString().ToLowerInvariant(),
        e.SaisonId,
        e.Saison?.Name,
        e.SpieltagNummer,
        e.AnzahlSpiele,
        e.AnzahlExakteTreffer,
        e.ExakteTrefferquote,
        e.AnzahlTendenzTreffer,
        e.TendenzTrefferquote,
        e.BerechnetAm);
}

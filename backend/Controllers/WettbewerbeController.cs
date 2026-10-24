using BetQuote.Api.Controllers.Dtos;
using BetQuote.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Controllers;

/// <summary>
/// Liste der aktiven Ziel-Wettbewerbe fuer die Land-&gt;Liga-Tab-Navigation im Wochen-Feed
/// (APP_STRUCTURE.md, PREDICTION_FACTORS.md "Ziel-Wettbewerbe"). Liefert bewusst NUR die drei
/// aktuell aktiven Wettbewerbe (1./2. Bundesliga, La Liga), nicht die historisch in der DB
/// belassenen 3. Liga/DFB-Pokal-Zeilen (siehe ScopeAenderungLaLigaUndKonfidenz-Migration) - diese
/// sind zwar weiterhin ueber /api/tabelle/{id} direkt abrufbar, sollen aber nicht mehr als
/// waehlbarer Tab im Frontend auftauchen.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WettbewerbeController(BetQuoteDbContext db) : ControllerBase
{
    /// <summary>GET /api/wettbewerbe - aktive Wettbewerbe, gruppierbar nach Land im Frontend (Deutschland: 1./2. Bundesliga, Spanien: La Liga).</summary>
    [HttpGet]
    public async Task<ActionResult<List<WettbewerbDto>>> GetAktive(CancellationToken ct)
    {
        var wettbewerbe = await db.Wettbewerbe
            .AsNoTracking()
            .Where(w => w.IstAktiv)
            .OrderBy(w => w.Sortierung)
            .Select(w => new WettbewerbDto(w.Id, w.Name, w.Kurzname, w.Land, w.IstPokal, w.Sortierung))
            .ToListAsync(ct);

        return Ok(wettbewerbe);
    }
}

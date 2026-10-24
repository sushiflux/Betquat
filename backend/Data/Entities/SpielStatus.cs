namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Spielstatus, abgeleitet aus OpenLigaDBs matchIsFinished-Flag plus Live-Erkennung
/// (Anstosszeit in der Vergangenheit, aber noch nicht beendet), siehe APP_STRUCTURE.md
/// "matchIsFinished-Flag -> damit laesst sich sauber zwischen 'vergangen', 'live' und 'kommend'
/// unterscheiden".
/// </summary>
public enum SpielStatus
{
    Geplant = 0,
    Live = 1,
    Beendet = 2,

    /// <summary>Abgesagt/verlegt ohne neuen Termin - kommt bei DFB-Pokal/Wetterlagen vor, eigener Status statt Datenluecke.</summary>
    Abgesagt = 3,
}

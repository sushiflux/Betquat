using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using RadioLinkEntity = BetQuote.Api.Data.Entities.RadioLink;

namespace BetQuote.Api.Services.RadioLink;

public interface IRadioLinkImportService
{
    /// <summary>Scraped die aktuelle Sportschau-Audio-Uebersicht und ordnet gefundene Streams den passenden Spielen zu (Einzelspiel- und/oder Konferenz-Link). Best-effort - siehe Klassendoku.</summary>
    Task<int> ImportAktuelleRadioLinksAsync(CancellationToken ct = default);
}

/// <summary>
/// Matched die von <see cref="ISportschauScraperService"/> gelieferten Audio-Eintraege gegen
/// unsere Spiele (per Team-Namen-Erwaehnung im Titel + Datum-Naehe, da die Sportschau-Seite keine
/// stabile externe Spiel-Id liefert) und schreibt/aktualisiert die RadioLink-Zeile. Rein additiv
/// und best-effort (Aufgabenstellung Punkt 6: "darf beim Fehlschlagen den Rest der App nicht
/// beeintraechtigen") - ein nicht zuordenbarer Audio-Eintrag wird einfach uebersprungen, kein
/// Fehlerzustand in der UI (siehe APP_STRUCTURE.md: "Icon erscheint dann schlicht nicht").
/// </summary>
public class RadioLinkImportService(
    ISportschauScraperService scraper,
    BetQuoteDbContext db,
    ILogger<RadioLinkImportService> logger) : IRadioLinkImportService
{
    public async Task<int> ImportAktuelleRadioLinksAsync(CancellationToken ct = default)
    {
        try
        {
            var audios = await scraper.GetAktuelleAudiosAsync(ct);
            if (audios.Count == 0)
            {
                return 0;
            }

            // Nur Spiele in einem plausiblen Zeitfenster um "jetzt" laden (Sportschau listet nur
            // aktuelle/kommende Uebertragungen, keine Alt-Historie) - vermeidet, die komplette
            // Spieletabelle fuer den Match-Versuch laden zu muessen.
            var von = DateTimeOffset.UtcNow.AddDays(-2);
            var bis = DateTimeOffset.UtcNow.AddDays(9);

            var kandidatenSpiele = await db.Spiele
                .Include(s => s.HeimTeam)
                .Include(s => s.AuswaertsTeam)
                .Include(s => s.RadioLink)
                .Where(s => s.Anstoss >= von && s.Anstoss <= bis)
                .ToListAsync(ct);

            var zugeordnet = 0;

            foreach (var audio in audios)
            {
                try
                {
                    var spiel = FindePassendesSpiel(audio, kandidatenSpiele);
                    if (spiel is null)
                    {
                        continue;
                    }

                    var istNeu = spiel.RadioLink is null;
                    spiel.RadioLink ??= new RadioLinkEntity { SpielId = spiel.Id };

                    if (audio.IstKonferenz)
                    {
                        spiel.RadioLink.KonferenzUrl = audio.Url;
                    }
                    else
                    {
                        spiel.RadioLink.EinzelspielUrl = audio.Url;
                    }
                    spiel.RadioLink.ErmitteltAm = DateTimeOffset.UtcNow;

                    if (istNeu)
                    {
                        db.RadioLinks.Add(spiel.RadioLink);
                    }

                    zugeordnet++;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "RadioLink-Zuordnung fuer Audio-Eintrag '{Titel}' fehlgeschlagen, uebersprungen", audio.Titel);
                }
            }

            if (zugeordnet > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            return zugeordnet;
        }
        catch (Exception ex)
        {
            // Aeusserster Schutzwall (Aufgabenstellung): egal was schiefgeht, der Rest der App
            // (Import/Vorhersage/API) darf davon nie betroffen sein.
            logger.LogWarning(ex, "RadioLink-Import komplett fehlgeschlagen - Feature bleibt fuer diesen Lauf leer");
            return 0;
        }
    }

    /// <summary>
    /// Fuzzy-Matching: beide Team-Kurznamen/Namen muessen im Audiotitel vorkommen (Reihenfolge
    /// egal, da Sportschau teils "Heim gegen Auswaerts" formuliert) UND der Anstoss muss zeitlich
    /// nah am gemeldeten Sendezeitpunkt liegen (Toleranz 3 Stunden - deckt leicht abweichende
    /// Sendestart- vs. Anstosszeiten ab).
    /// </summary>
    private static Spiel? FindePassendesSpiel(SportschauAudioEintrag audio, List<Spiel> kandidaten)
    {
        return kandidaten.FirstOrDefault(spiel =>
        {
            var heimTreffer = TitelEnthaeltTeam(audio.Titel, spiel.HeimTeam);
            var auswaertsTreffer = TitelEnthaeltTeam(audio.Titel, spiel.AuswaertsTeam);

            if (!heimTreffer || !auswaertsTreffer)
            {
                return false;
            }

            if (audio.Anstoss is null)
            {
                return true; // Kein Zeitpunkt bekannt - Team-Match allein reicht als Best-Effort.
            }

            return Math.Abs((audio.Anstoss.Value - spiel.Anstoss).TotalHours) <= 3;
        });
    }

    private static bool TitelEnthaeltTeam(string titel, Team team)
    {
        var kandidatenNamen = new[] { team.Name, team.Kurzname }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(VereinfachtenNamen);

        var vereinfachterTitel = VereinfachtenNamen(titel);

        return kandidatenNamen.Any(n => !string.IsNullOrEmpty(n) && vereinfachterTitel.Contains(n!, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Reduziert Vereinsnamen auf ein vergleichbares Kernwort (z.B. "FC Bayern München" -&gt; "bayern") - Praefixe/Rechtsformen wie "FC"/"1."/"SC" entfernt, die in Sportschau-Titeln oft weggelassen werden.</summary>
    private static string VereinfachtenNamen(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var woerter = name
            .Replace(".", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => !BekanntePraefixe.Contains(w.ToLowerInvariant()))
            .ToList();

        return woerter.Count > 0 ? woerter[^1].ToLowerInvariant() : name.ToLowerInvariant();
    }

    private static readonly HashSet<string> BekanntePraefixe = new(StringComparer.OrdinalIgnoreCase)
    {
        "fc", "1", "sc", "sv", "vfl", "vfb", "tsg", "rb", "fsv", "sg", "spvgg", "kfc", "borussia", "tsv", "eintracht", "hertha", "bsc", "werder", "bayer", "greuther",
    };
}

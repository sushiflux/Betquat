using System.Net;
using System.Text.Json;
using HtmlAgilityPack;

namespace BetQuote.Api.Services.RadioLink;

public interface ISportschauScraperService
{
    /// <summary>Scraped die ARD-Sportschau-Audio-Uebersichtsseite und liefert alle dort aktuell gelisteten Fussball-Audiostreams (Einzelspiel + Konferenz). Wirft NICHT bei Scraping-Fehlern - siehe Klassendoku.</summary>
    Task<List<SportschauAudioEintrag>> GetAktuelleAudiosAsync(CancellationToken ct = default);
}

/// <summary>
/// Scraping-Modul fuer die ARD-Sportschau-Audiostream-Uebersichtsseite (TECH_STACK.md/
/// APP_STRUCTURE.md "Radio-Link pro Spiel"). Bewusst niedrigste Prioritaet, rein optional und
/// sauber gekapselt (Aufgabenstellung: "darf beim Fehlschlagen den Rest der App nicht
/// beeinfluassen") - jede Methode faengt alle Exceptions ab und gibt im Fehlerfall schlicht eine
/// leere Liste zurueck statt zu werfen, damit ein Aufrufer nie try/catch drumherum braucht.
///
/// Technischer Ansatz: keine stabile/dokumentierte API vorhanden (siehe TECH_STACK.md - "keine
/// offizielle API gefunden"), daher HTML-Scraping der oeffentlichen Uebersichtsseite
/// https://www.sportschau.de/audio/. Jeder Audioplayer-Block traegt ein data-v-Attribut mit
/// eingebettetem JSON (Titel, Original-Sendezeit, Permalink) - robuster als Text-Scraping der
/// sichtbaren Seite, aber weiterhin eine unoffizielle, jederzeit aenderbare Seitenstruktur (siehe
/// PREDICTION_FACTORS.md Abschnitt 10 fuer dieselbe Einschraenkung bei Sofascore).
/// </summary>
public class SportschauScraperService(HttpClient httpClient, ILogger<SportschauScraperService> logger) : ISportschauScraperService
{
    private const string UebersichtsseiteUrl = "https://www.sportschau.de/audio/";

    public async Task<List<SportschauAudioEintrag>> GetAktuelleAudiosAsync(CancellationToken ct = default)
    {
        try
        {
            var html = await httpClient.GetStringAsync(UebersichtsseiteUrl, ct);
            return ParseAudioEintraege(html);
        }
        catch (Exception ex)
        {
            // Kein Rethrow (Klassendoku): Scraping-Fehler duerfen die App nicht beeintraechtigen,
            // Radio-Icons bleiben in dem Fall in der UI einfach schlicht weg.
            logger.LogWarning(ex, "Sportschau-Audio-Scraping fehlgeschlagen - Radio-Links bleiben fuer diesen Lauf leer");
            return [];
        }
    }

    private List<SportschauAudioEintrag> ParseAudioEintraege(string html)
    {
        var ergebnis = new List<SportschauAudioEintrag>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var playerKnoten = doc.DocumentNode.SelectNodes("//div[contains(@class,'v-instance')]");
        if (playerKnoten is null)
        {
            return ergebnis;
        }

        foreach (var knoten in playerKnoten)
        {
            try
            {
                var eintrag = ParseEinzelnenKnoten(knoten);
                if (eintrag is not null)
                {
                    ergebnis.Add(eintrag);
                }
            }
            catch (Exception ex)
            {
                // Ein einzelner nicht parsebarer Block (z.B. unerwartetes/fehlendes JSON) darf die
                // uebrigen Eintraege nicht verhindern.
                logger.LogDebug(ex, "Einzelner Sportschau-Audio-Block konnte nicht geparst werden, uebersprungen");
            }
        }

        return ergebnis;
    }

    private static SportschauAudioEintrag? ParseEinzelnenKnoten(HtmlNode knoten)
    {
        var dataV = knoten.GetAttributeValue("data-v", null);
        if (string.IsNullOrWhiteSpace(dataV))
        {
            return null;
        }

        // HTML-Entities (&quot; etc.) im Attributwert dekodieren, bevor als JSON geparst wird.
        var json = WebUtility.HtmlDecode(dataV);

        using var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;

        if (!root.TryGetProperty("mc", out var mc))
        {
            return null;
        }

        var titel = mc.TryGetProperty("meta", out var meta) && meta.TryGetProperty("title", out var titleEl)
            ? titleEl.GetString()
            : null;

        // Nur Fussball-relevante Eintraege interessieren uns - die Uebersichtsseite fuehrt auch
        // andere Sportarten (siehe curl-Stichprobe: Radsport, Tennis etc.). "seriesTitle"/"synopsis"
        // enthalten typischerweise Liga-/Spiel-Bezeichner wie "Bundesliga", "2. Liga", "3. Liga",
        // "DFB-Pokal", "Konferenz".
        var seriesTitle = meta.TryGetProperty("seriesTitle", out var seriesEl) ? seriesEl.GetString() ?? "" : "";
        var relevantesStichwort = new[] { "Bundesliga", "3. Liga", "DFB-Pokal", "Fußball" }
            .Any(s => seriesTitle.Contains(s, StringComparison.OrdinalIgnoreCase) || (titel?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));

        if (!relevantesStichwort || string.IsNullOrWhiteSpace(titel))
        {
            return null;
        }

        DateTimeOffset? anstoss = null;
        if (meta.TryGetProperty("broadcastedOnDateTime", out var dtEl)
            && DateTimeOffset.TryParse(dtEl.GetString(), out var parsedDt))
        {
            anstoss = parsedDt;
        }

        var url = root.TryGetProperty("pluginData", out var pluginData)
                  && pluginData.TryGetProperty("sharing@web", out var sharing)
                  && sharing.TryGetProperty("link", out var linkEl)
            ? linkEl.GetString()?.Split("$params$")[0]
            : null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var istKonferenz = titel.Contains("Konferenz", StringComparison.OrdinalIgnoreCase)
                            || seriesTitle.Contains("Konferenz", StringComparison.OrdinalIgnoreCase);

        return new SportschauAudioEintrag(titel, anstoss, url, istKonferenz);
    }
}

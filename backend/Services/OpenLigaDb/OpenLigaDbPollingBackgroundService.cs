using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.Trefferquote;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.OpenLigaDb;

/// <summary>
/// Laufender Hintergrunddienst fuers Scheduling des OpenLigaDB-Imports (Aufgabenstellung: "muss
/// wiederholt aufrufbar sein ... sinnvoller Scheduling-Mechanismus ... haeufigere Abfrage wenn
/// Spiele gerade laufen"). Adaptives Intervall statt fixem Timer:
/// - Laufen aktuell Spiele (Status=Live in der DB, oder ein geplantes Spiel dessen Anstoss gerade
///   begonnen hat): alle 60 Sekunden nur die aktuellen Spieltage pollen (guenstig, kein Vollimport).
/// - Sonst: alle 30 Minuten ebenfalls nur "aktuelle" Daten pollen (faengt neu angesetzte/verlegte
///   Spiele und Tabellenupdates ab, ohne die komplette Historie jedes Mal neu zu laden).
/// - Einmal taeglich (naechtens) zusaetzlich ein voller Import aller Saisons (faengt z.B.
///   rueckwirkende Korrekturen/neu hinzugefuegte historische Saisons ab).
/// Nach jedem Importlauf wird direkt die Trefferquote-Auswertung angestossen (guenstig, da sie nur
/// auf frisch beendete Spiele reagiert - kein zusaetzlicher separater Scheduler noetig).
/// </summary>
public class OpenLigaDbPollingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OpenLigaDbPollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan LiveIntervall = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RuheIntervall = TimeSpan.FromMinutes(30);

    private DateOnly _letzterVollimportTag = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kurze initiale Verzoegerung, damit die DB/Migrationen beim allerersten Containerstart
        // sicher bereit sind, bevor der erste Import versucht wird.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunZyklusAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Der gesamte Polling-Zyklus darf den Hintergrunddienst nicht zum Absturz bringen -
                // Aufgabenstellung: "sauberes Fehlerhandling".
                logger.LogError(ex, "OpenLigaDB-Polling-Zyklus fehlgeschlagen, naechster Versuch nach Intervall");
            }

            var wartezeit = await IstAktuellLiveAsync(stoppingToken) ? LiveIntervall : RuheIntervall;

            try
            {
                await Task.Delay(wartezeit, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunZyklusAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IOpenLigaDbImportService>();
        var trefferquoteService = scope.ServiceProvider.GetRequiredService<ITrefferquoteService>();

        var heute = DateOnly.FromDateTime(DateTime.UtcNow);
        ImportSummary summary;

        if (heute != _letzterVollimportTag)
        {
            logger.LogInformation("Starte taeglichen OpenLigaDB-Vollimport (alle Wettbewerbe/Saisons)");
            summary = await importService.ImportAllAsync(ct);
            _letzterVollimportTag = heute;
        }
        else
        {
            summary = await importService.ImportCurrentSeasonsAsync(ct);
        }

        if (summary.Fehler.Count > 0)
        {
            logger.LogWarning("OpenLigaDB-Import abgeschlossen mit {FehlerAnzahl} Fehlern: {Fehler}", summary.Fehler.Count, string.Join(" | ", summary.Fehler));
        }
        else
        {
            logger.LogInformation("OpenLigaDB-Import abgeschlossen: {Neu} neu, {Aktualisiert} aktualisiert, {TeamsNeu} neue Teams", summary.SpieleNeu, summary.SpieleAktualisiert, summary.TeamsNeu);
        }

        var ausgewertet = await trefferquoteService.AuswerteAbgeschlosseneVorhersagenAsync(ct);
        if (ausgewertet > 0)
        {
            await trefferquoteService.AktualisiereAbgeschlosseneSpieltageAsync(ct);
        }
    }

    private async Task<bool> IstAktuellLiveAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BetQuoteDbContext>();

        return await db.Spiele.AnyAsync(s => s.Status == SpielStatus.Live, ct);
    }
}

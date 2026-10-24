using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.OpenLigaDb;
using BetQuote.Api.Services.Trefferquote;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.FootballDataOrg;

/// <summary>
/// Scheduling-Analogon zu <see cref="OpenLigaDb.OpenLigaDbPollingBackgroundService"/> fuer La Liga
/// (siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe" - Scope-Aenderung). Deutlich groessere
/// Intervalle als beim OpenLigaDB-Pendant, weil football-data.org im kostenlosen Tier nur 10
/// Requests/Minute erlaubt (siehe FootballDataOrgClient) - haeufiges Polling wuerde das Limit
/// sofort reissen. Ohne konfigurierten API-Key ueberspringt der zugrunde liegende Importer jeden
/// Zyklus mit einer klaren Log-Meldung (kein Fehler, kein Blocker fuer den Rest der App).
/// </summary>
public class FootballDataOrgPollingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<FootballDataOrgPollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan LiveIntervall = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RuheIntervall = TimeSpan.FromHours(1);

    private DateOnly _letzterVollimportTag = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
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
                logger.LogError(ex, "football-data.org-Polling-Zyklus fehlgeschlagen, naechster Versuch nach Intervall");
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
        var importService = scope.ServiceProvider.GetRequiredService<IFootballDataOrgImportService>();
        var trefferquoteService = scope.ServiceProvider.GetRequiredService<ITrefferquoteService>();

        var heute = DateOnly.FromDateTime(DateTime.UtcNow);
        ImportSummary summary;

        if (heute != _letzterVollimportTag)
        {
            summary = await importService.ImportAllAsync(ct);
            _letzterVollimportTag = heute;
        }
        else
        {
            summary = await importService.ImportCurrentAsync(ct);
        }

        if (summary.Fehler.Count > 0)
        {
            logger.LogInformation("football-data.org-Import: {FehlerAnzahl} Meldung(en): {Fehler}", summary.Fehler.Count, string.Join(" | ", summary.Fehler));
        }
        else if (summary.SpieleNeu > 0 || summary.SpieleAktualisiert > 0)
        {
            logger.LogInformation("football-data.org-Import abgeschlossen: {Neu} neu, {Aktualisiert} aktualisiert", summary.SpieleNeu, summary.SpieleAktualisiert);
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

        return await db.Spiele
            .Where(s => s.Wettbewerb.FootballDataOrgCode != null)
            .AnyAsync(s => s.Status == SpielStatus.Live, ct);
    }
}

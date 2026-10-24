namespace BetQuote.Api.Services.RadioLink;

/// <summary>
/// Periodischer Hintergrunddienst fuers Radio-Link-Scraping (Aufgabenstellung Punkt 6, niedrigste
/// Prioritaet). Laeuft alle 3 Stunden - haeufig genug, um neu online gestellte Audiostreams fuer
/// die kommenden Spieltage zeitnah zu erfassen, aber ohne die Sportschau-Seite unnoetig oft
/// abzufragen. Komplett best-effort: ein Fehlschlag wird geloggt, nie propagiert (siehe
/// RadioLinkImportService/SportschauScraperService-Dokumentation).
/// </summary>
public class RadioLinkBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RadioLinkBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervall = TimeSpan.FromHours(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var radioLinkService = scope.ServiceProvider.GetRequiredService<IRadioLinkImportService>();

                var anzahl = await radioLinkService.ImportAktuelleRadioLinksAsync(stoppingToken);
                if (anzahl > 0)
                {
                    logger.LogInformation("Radio-Link-Scraping: {Anzahl} Spiele mit Audiostream-Link versehen", anzahl);
                }
            }
            catch (Exception ex)
            {
                // Auch hier: darf den restlichen Betrieb nie stoeren (Aufgabenstellung).
                logger.LogWarning(ex, "Radio-Link-Scraping-Zyklus fehlgeschlagen, naechster Versuch nach Intervall");
            }

            try
            {
                await Task.Delay(Intervall, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}

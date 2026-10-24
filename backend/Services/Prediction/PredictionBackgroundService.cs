namespace BetQuote.Api.Services.Prediction;

/// <summary>
/// Taeglicher Batch-Lauf, der Vorhersagen fuer alle Spiele der naechsten 7 Tage ohne aktuelle
/// Vorhersage berechnet (Aufgabenstellung Punkt 3: "Vorhersage MUSS vor Anpfiff berechnet werden
/// ... Job das rechtzeitig vor Anstoss laeuft, z.B. taeglich"). Laeuft bewusst als eigener,
/// haeufigerer Zyklus (alle 6 Stunden) statt nur einmal pro Tag: faengt neu importierte Spiele
/// (z.B. neu terminierte Nachholspiele/Pokal-Ansetzungen) zeitnah ab, ohne auf den naechsten
/// Kalendertag warten zu muessen.
/// </summary>
public class PredictionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PredictionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervall = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
                var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();

                var anzahl = await predictionService.BerechneAusstehendeVorhersagenAsync(7, stoppingToken);
                if (anzahl > 0)
                {
                    logger.LogInformation("Vorhersage-Batch-Lauf: {Anzahl} neue Vorhersagen berechnet", anzahl);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Vorhersage-Batch-Lauf fehlgeschlagen, naechster Versuch nach Intervall");
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

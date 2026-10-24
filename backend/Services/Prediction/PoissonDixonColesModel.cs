namespace BetQuote.Api.Services.Prediction;

public record ErgebnisWahrscheinlichkeit(int ToreHeim, int ToreAuswaerts, decimal Wahrscheinlichkeit);

public record VorhersageErgebnis(
    List<ErgebnisWahrscheinlichkeit> Verteilung,
    ErgebnisWahrscheinlichkeit Haupttipp,
    decimal WahrscheinlichkeitHeimsieg,
    decimal WahrscheinlichkeitRemis,
    decimal WahrscheinlichkeitAuswaertssieg,
    decimal WahrscheinlichkeitUeber25,
    decimal WahrscheinlichkeitBeideTreffen,
    decimal LambdaHeim,
    decimal LambdaAuswaerts);

/// <summary>
/// Statistisches Grundgerüst fuer die Torerwartung (PREDICTION_FACTORS.md "Modellansatz", Stufe 1):
/// unabhaengige Poisson-Verteilungen fuer Heim-/Auswaertstore, mit einer Dixon-Coles-Korrektur fuer
/// niedrige Ergebnisse (0:0, 1:0, 0:1, 1:1), da die reine Poisson-Annahme (Heim-/Auswaertstore
/// vollstaendig unabhaengig) empirisch genau bei diesen torarmen Ergebnissen leicht daneben liegt -
/// Dixon &amp; Coles (1997) korrigieren das ueber einen kleinen Anpassungsfaktor Tau.
/// </summary>
public static class PoissonDixonColesModel
{
    /// <summary>Rho-Parameter der Dixon-Coles-Korrektur - Literaturwert aus dem Originalpaper (Dixon &amp; Coles 1997), leicht negativ (negative Korrelation bei torarmen Ergebnissen).</summary>
    private const double Rho = -0.13;

    /// <summary>Maximal betrachtete Tore je Team in der Verteilung - deckt praktisch 99,9%+ der Wahrscheinlichkeitsmasse ab, hoehere Ergebnisse sind vernachlaessigbar selten.</summary>
    private const int MaxTore = 8;

    public static VorhersageErgebnis Berechne(decimal lambdaHeim, decimal lambdaAuswaerts)
    {
        var lh = (double)lambdaHeim;
        var la = (double)lambdaAuswaerts;

        var rohMatrix = new double[MaxTore + 1, MaxTore + 1];
        double summe = 0;

        for (var h = 0; h <= MaxTore; h++)
        {
            for (var a = 0; a <= MaxTore; a++)
            {
                var p = PoissonPmf(h, lh) * PoissonPmf(a, la) * DixonColesTau(h, a, lh, la);
                rohMatrix[h, a] = p;
                summe += p;
            }
        }

        var verteilung = new List<ErgebnisWahrscheinlichkeit>();
        for (var h = 0; h <= MaxTore; h++)
        {
            for (var a = 0; a <= MaxTore; a++)
            {
                // Normalisieren, da die Dixon-Coles-Tau-Korrektur und das Abschneiden bei MaxTore
                // die Summe leicht von 1 abweichen lassen koennen.
                var normalisiert = summe > 0 ? rohMatrix[h, a] / summe : 0;
                verteilung.Add(new ErgebnisWahrscheinlichkeit(h, a, (decimal)Math.Round(normalisiert, 6)));
            }
        }

        verteilung = [.. verteilung.OrderByDescending(v => v.Wahrscheinlichkeit)];
        var haupttipp = verteilung[0];

        var heimsieg = verteilung.Where(v => v.ToreHeim > v.ToreAuswaerts).Sum(v => v.Wahrscheinlichkeit);
        var remis = verteilung.Where(v => v.ToreHeim == v.ToreAuswaerts).Sum(v => v.Wahrscheinlichkeit);
        var auswaertssieg = verteilung.Where(v => v.ToreHeim < v.ToreAuswaerts).Sum(v => v.Wahrscheinlichkeit);
        var ueber25 = verteilung.Where(v => v.ToreHeim + v.ToreAuswaerts > 2).Sum(v => v.Wahrscheinlichkeit);
        var beideTreffen = verteilung.Where(v => v.ToreHeim > 0 && v.ToreAuswaerts > 0).Sum(v => v.Wahrscheinlichkeit);

        return new VorhersageErgebnis(
            verteilung,
            haupttipp,
            Math.Round(heimsieg, 5),
            Math.Round(remis, 5),
            Math.Round(auswaertssieg, 5),
            Math.Round(ueber25, 5),
            Math.Round(beideTreffen, 5),
            lambdaHeim,
            lambdaAuswaerts);
    }

    private static double PoissonPmf(int k, double lambda)
    {
        if (lambda <= 0) lambda = 0.01; // Degenerierte Faelle (Lambda 0) vermeiden - minimal positive Torerwartung.
        return Math.Exp(-lambda) * Math.Pow(lambda, k) / Factorial(k);
    }

    private static double Factorial(int n)
    {
        double result = 1;
        for (var i = 2; i <= n; i++) result *= i;
        return result;
    }

    /// <summary>Dixon-Coles-Korrekturfaktor Tau - wirkt nur auf die vier torarmen Zellen (0-0, 1-0, 0-1, 1-1), ueberall sonst 1 (keine Korrektur).</summary>
    private static double DixonColesTau(int h, int a, double lambdaHeim, double lambdaAuswaerts)
    {
        return (h, a) switch
        {
            (0, 0) => 1 - lambdaHeim * lambdaAuswaerts * Rho,
            (0, 1) => 1 + lambdaHeim * Rho,
            (1, 0) => 1 + lambdaAuswaerts * Rho,
            (1, 1) => 1 - Rho,
            _ => 1.0,
        };
    }
}

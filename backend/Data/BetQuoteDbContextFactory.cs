using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BetQuote.Api.Data;

/// <summary>
/// Design-Time-Factory, ausschliesslich fuer "dotnet ef migrations add/remove/script" ausserhalb
/// des Docker-Containers auf diesem Windows-Rechner. Braucht keine echte DB-Verbindung (EF Core
/// generiert Migrationen rein aus dem Modell), aber UseNpgsql() verlangt einen syntaktisch
/// gueltigen Connection-String - deshalb ein Platzhalter-String statt des echten, per Docker-
/// Compose-Secret verwalteten Passworts (siehe Program.cs). Zur Laufzeit im Container wird
/// stattdessen ganz reguleaer builder.Services.AddDbContext(...) aus Program.cs verwendet, diese
/// Factory kommt dort nie zum Einsatz.
/// </summary>
public class BetQuoteDbContextFactory : IDesignTimeDbContextFactory<BetQuoteDbContext>
{
    public BetQuoteDbContext CreateDbContext(string[] args)
    {
        // Erlaubt "dotnet ef database update" gegen die echte, per start.ps1 laufende
        // Dev-Datenbank (Host-Port aus der Port-Registry, siehe .env) - Override per Env-Var statt
        // das echte Passwort hart im Quellcode zu hinterlegen. Ohne gesetzte Env-Var faellt das auf
        // einen rein syntaktischen Platzhalter zurueck, der fuer "migrations add" (kein echter
        // DB-Zugriff noetig) ausreicht.
        var connectionString =
            Environment.GetEnvironmentVariable("BETQUOTE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=betquote;Username=betquote;Password=design-time-only";

        var optionsBuilder = new DbContextOptionsBuilder<BetQuoteDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new BetQuoteDbContext(optionsBuilder.Options);
    }
}

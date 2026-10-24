using BetQuote.Api.Data;
using BetQuote.Api.Services.Elo;
using BetQuote.Api.Services.FootballDataOrg;
using BetQuote.Api.Services.OpenLigaDb;
using BetQuote.Api.Services.Prediction;
using BetQuote.Api.Services.RadioLink;
using BetQuote.Api.Services.Trefferquote;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// BetQuote ist ein reines Einzelnutzer-Tool ohne Login/Auth (siehe CLAUDE.md-Vorgabe fuer dieses
// Projekt) - bewusst kein Authentication/Authorization-Setup hier.

// DB-Connection-String zur Laufzeit aus Host/Port/DB/User (Klartext-Env-Vars, unkritisch) plus
// Passwort aus dem per Docker-Compose-Secret gemounteten File zusammenbauen - das Passwort
// selbst landet dadurch nie als Klartext-Env-Var im Container (siehe CLAUDE.md "Secrets
// management fuer lokale Docker Compose Stacks"). Fallback auf ConnectionStrings:Default aus
// appsettings.json, falls das Projekt mal ausserhalb von Docker/ohne Secret-File laeuft.
var pgPasswordFile = builder.Configuration["POSTGRES_PASSWORD_FILE"];
if (!string.IsNullOrWhiteSpace(pgPasswordFile) && File.Exists(pgPasswordFile))
{
    var pgHost = builder.Configuration["POSTGRES_HOST"] ?? "postgres";
    var pgPort = builder.Configuration["POSTGRES_PORT"] ?? "5432";
    var pgDb = builder.Configuration["POSTGRES_DB"] ?? "betquote";
    var pgUser = builder.Configuration["POSTGRES_USER"] ?? "betquote";
    var pgPassword = File.ReadAllText(pgPasswordFile).Trim();

    builder.Configuration["ConnectionStrings:Default"] =
        $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPassword}";
}

const string DevCorsPolicy = "DevCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5192",
                "https://betquote.localhost",
                "http://betquote.localhost")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
// Mehr zur OpenAPI-Konfiguration: https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// UseSnakeCaseNamingConvention (EFCore.NamingConventions): mappt alle C#-PascalCase-Property-
// /Tabellennamen automatisch auf snake_case-SQL-Identifier (siehe Data/Configurations/* fuer
// CHECK-Constraints, die bereits auf die snake_case-Spaltennamen verweisen) - idiomatisch fuer
// Postgres statt per-Property haendisch HasColumnName() zu setzen.
builder.Services.AddDbContext<BetQuoteDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

// OpenLigaDB-Client: kostenlose, unauthentifizierte Primaerquelle (siehe PREDICTION_FACTORS.md
// "Datenquellen"/TECH_STACK.md). Ein 20s-Timeout statt des HttpClient-Defaults (100s), damit ein
// haengender Request den Import-Zyklus nicht unnoetig lange blockiert (Fehlerhandling faengt das
// im Importer selbst ab, siehe OpenLigaDbImportService).
builder.Services.AddHttpClient<IOpenLigaDbClient, OpenLigaDbClient>(client =>
{
    client.BaseAddress = new Uri("https://api.openligadb.de/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddScoped<IOpenLigaDbImportService, OpenLigaDbImportService>();
builder.Services.AddScoped<IEloRatingService, EloRatingService>();
builder.Services.AddScoped<ITeamFormService, TeamFormService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<ITrefferquoteService, TrefferquoteService>();

// football-data.org-Client: Primaerquelle fuer La Liga (siehe PREDICTION_FACTORS.md "Ziel-
// Wettbewerbe" - Scope-Aenderung, OpenLigaDB deckt La Liga nur unzuverlaessig ab). Erfordert einen
// per Umgebungsvariable FOOTBALL_DATA_ORG_API_KEY gesetzten Key (siehe FootballDataOrgClient fuer
// den offenen Punkt "Nutzer muss den Key manuell registrieren").
builder.Services.AddHttpClient<IFootballDataOrgClient, FootballDataOrgClient>(client =>
{
    client.BaseAddress = new Uri("https://api.football-data.org/v4/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IFootballDataOrgImportService, FootballDataOrgImportService>();

// Radio-Link-Scraping (optional/niedrigste Prioritaet, siehe Aufgabenstellung Punkt 6) - eigener
// HttpClient mit Browser-typischem User-Agent, da manche Seiten Requests ohne UA blocken/anders
// ausliefern; Timeout etwas grosszuegiger als beim OpenLigaDB-Client, da das Scraping-Ziel nicht
// unter unserer Kontrolle steht.
builder.Services.AddHttpClient<ISportschauScraperService, SportschauScraperService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BetQuote/1.0; privates Vorhersage-Tool)");
});
builder.Services.AddScoped<IRadioLinkImportService, RadioLinkImportService>();

// Hintergrunddienste fuer den laufenden Betrieb (Datenimport-Scheduling, taegliche
// Vorhersage-Berechnung) - siehe jeweilige Klassendoku fuer das genaue Intervall-Konzept.
builder.Services.AddHostedService<OpenLigaDbPollingBackgroundService>();
builder.Services.AddHostedService<FootballDataOrgPollingBackgroundService>();
builder.Services.AddHostedService<PredictionBackgroundService>();
builder.Services.AddHostedService<RadioLinkBackgroundService>();

var app = builder.Build();

// Ausstehende Migrationen beim Start automatisch anwenden - unkritisch fuer dieses Ein-Personen-
// Projekt ohne parallele Deploys/Downtime-Anforderungen (kein Blue-Green-Deployment o.ae.), spart
// den manuellen "dotnet ef database update"-Schritt nach jedem Pull/Codeaenderung am Datenmodell.
using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<BetQuoteDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}

// Kein UseHttpsRedirection: TLS wird von Caddy terminiert (siehe docker/caddy/Caddyfile),
// das Backend selbst spricht intern nur HTTP im Docker-Netzwerk.

// Standard-Middleware-Reihenfolge gemaess CLAUDE.md (UseAuthentication/UseAuthorization
// entfallen hier bewusst, da dieses Projekt kein Login/Auth-System hat).
app.MapControllers();

app.Run();

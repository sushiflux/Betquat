using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Data;

/// <summary>
/// EF-Core-DbContext fuer BetQuote. Code-First: Entities in Data/Entities, Fluent-API-Konfiguration
/// (Indizes, Constraints, Praezision) in Data/Configurations, jeweils eine Datei pro Entity - siehe
/// Migrations-Ordner fuer den generierten SQL-Schema-Verlauf.
/// </summary>
public class BetQuoteDbContext(DbContextOptions<BetQuoteDbContext> options) : DbContext(options)
{
    public DbSet<Wettbewerb> Wettbewerbe => Set<Wettbewerb>();
    public DbSet<Saison> Saisons => Set<Saison>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Datenquelle> Datenquellen => Set<Datenquelle>();
    public DbSet<TeamAlias> TeamAliase => Set<TeamAlias>();
    public DbSet<Spiel> Spiele => Set<Spiel>();
    public DbSet<TorEvent> TorEvents => Set<TorEvent>();
    public DbSet<Spieler> Spieler => Set<Spieler>();
    public DbSet<SpielerVerletzung> SpielerVerletzungen => Set<SpielerVerletzung>();
    public DbSet<Schiedsrichter> Schiedsrichter => Set<Schiedsrichter>();
    public DbSet<SchiedsrichterStatistik> SchiedsrichterStatistiken => Set<SchiedsrichterStatistik>();
    public DbSet<EloRatingHistorie> EloRatingHistorien => Set<EloRatingHistorie>();
    public DbSet<Vorhersage> Vorhersagen => Set<Vorhersage>();
    public DbSet<TrefferquoteEintrag> TrefferquoteEintraege => Set<TrefferquoteEintrag>();
    public DbSet<XgStatistik> XgStatistiken => Set<XgStatistik>();
    public DbSet<ZeitsegmentStatistik> ZeitsegmentStatistiken => Set<ZeitsegmentStatistik>();
    public DbSet<RadioLink> RadioLinks => Set<RadioLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BetQuoteDbContext).Assembly);
    }
}

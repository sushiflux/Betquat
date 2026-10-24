using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class DatenquelleConfiguration : IEntityTypeConfiguration<Datenquelle>
{
    public void Configure(EntityTypeBuilder<Datenquelle> builder)
    {
        builder.ToTable("datenquellen");

        builder.Property(d => d.Schluessel).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Anzeigename).HasMaxLength(100).IsRequired();

        builder.HasIndex(d => d.Schluessel).IsUnique();

        // Feste Startwerte, siehe TECH_STACK.md "Externe Datenquellen" - direkt per Migration
        // vorbelegt, damit Import-Module beim ersten Lauf sofort per Schluessel referenzieren
        // koennen, ohne selbst Insert-or-get-Logik fuer die Quelle selbst zu brauchen.
        builder.HasData(
            new Datenquelle { Id = 1, Schluessel = "openligadb", Anzeigename = "OpenLigaDB" },
            new Datenquelle { Id = 2, Schluessel = "kaggle", Anzeigename = "Kaggle AllBundesligaGamesEver" },
            new Datenquelle { Id = 3, Schluessel = "understat", Anzeigename = "Understat" },
            new Datenquelle { Id = 4, Schluessel = "sofascore", Anzeigename = "Sofascore" },
            new Datenquelle { Id = 5, Schluessel = "fbref", Anzeigename = "FBref" },
            new Datenquelle { Id = 6, Schluessel = "sportschau", Anzeigename = "ARD Sportschau" },
            // Neu (Scope-Aenderung La Liga, siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe").
            new Datenquelle { Id = 7, Schluessel = "footballdataorg", Anzeigename = "football-data.org" }
        );
    }
}

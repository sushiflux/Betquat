using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class WettbewerbConfiguration : IEntityTypeConfiguration<Wettbewerb>
{
    public void Configure(EntityTypeBuilder<Wettbewerb> builder)
    {
        builder.ToTable("wettbewerbe");

        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Kurzname).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Land).HasMaxLength(50).IsRequired();
        builder.Property(w => w.OpenLigaDbKuerzel).HasMaxLength(20);
        builder.Property(w => w.FootballDataOrgCode).HasMaxLength(20);

        // Kuerzel/Codes muessen eindeutig sein, damit Import-Upserts zuverlaessig funktionieren -
        // beide als partielle Unique-Indizes (nullable, da ein Wettbewerb jeweils nur ueber EINE
        // der beiden Quellen laeuft, siehe Wettbewerb-Entity-Dokumentation).
        builder.HasIndex(w => w.OpenLigaDbKuerzel).IsUnique().HasFilter("\"open_liga_db_kuerzel\" IS NOT NULL");
        builder.HasIndex(w => w.FootballDataOrgCode).IsUnique().HasFilter("\"football_data_org_code\" IS NOT NULL");
        builder.HasIndex(w => w.Name).IsUnique();

        // Feste Startwerte (PREDICTION_FACTORS.md "Ziel-Wettbewerbe": aktualisiert auf 1./2.
        // Bundesliga + La Liga, 3. Liga/DFB-Pokal wieder rausgenommen) - direkt per Migration
        // vorbelegt, analog zu DatenquelleConfiguration, damit die Importer beim ersten Lauf sofort
        // alle 3 Wettbewerbe vorfinden statt sie selbst anlegen zu muessen. Ids 3/4 (ehemals 3. Liga/
        // DFB-Pokal) bewusst nicht wiederverwendet, um bereits importierte historische Spiele/
        // Saisons dieser beiden Wettbewerbe nicht versehentlich neu zu interpretieren - La Liga
        // bekommt eine neue Id 5 statt 3 nachzunutzen.
        builder.HasData(
            new Wettbewerb { Id = 1, Name = "1. Bundesliga", Kurzname = "BL1", Land = "Deutschland", OpenLigaDbKuerzel = "bl1", IstPokal = false, IstAktiv = true, Sortierung = 1 },
            new Wettbewerb { Id = 2, Name = "2. Bundesliga", Kurzname = "BL2", Land = "Deutschland", OpenLigaDbKuerzel = "bl2", IstPokal = false, IstAktiv = true, Sortierung = 2 },
            new Wettbewerb { Id = 5, Name = "La Liga", Kurzname = "LIGA", Land = "Spanien", FootballDataOrgCode = "PD", IstPokal = false, IstAktiv = true, Sortierung = 3 }
        );
    }
}

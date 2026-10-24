using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class VorhersageConfiguration : IEntityTypeConfiguration<Vorhersage>
{
    public void Configure(EntityTypeBuilder<Vorhersage> builder)
    {
        builder.ToTable("vorhersagen");

        builder.Property(v => v.ModellVersion).HasMaxLength(100).IsRequired();
        builder.Property(v => v.TippWahrscheinlichkeit).HasPrecision(6, 5);
        builder.Property(v => v.WahrscheinlichkeitHeimsieg).HasPrecision(6, 5);
        builder.Property(v => v.WahrscheinlichkeitRemis).HasPrecision(6, 5);
        builder.Property(v => v.WahrscheinlichkeitAuswaertssieg).HasPrecision(6, 5);
        builder.Property(v => v.WahrscheinlichkeitUeber25).HasPrecision(6, 5);
        builder.Property(v => v.WahrscheinlichkeitBeideTreffen).HasPrecision(6, 5);
        builder.Property(v => v.KonfidenzScore).HasPrecision(6, 5);

        // JsonDocument -> jsonb: Npgsql unterstuetzt System.Text.Json.JsonDocument nativ als
        // jsonb-Spaltentyp (siehe Npgsql.EntityFrameworkCore.PostgreSQL-Doku) - kein manueller
        // Value-Converter noetig.
        builder.Property(v => v.Ergebnisverteilung).HasColumnType("jsonb").IsRequired();
        builder.Property(v => v.FaktorenSnapshot).HasColumnType("jsonb").IsRequired();

        builder.HasOne(v => v.Spiel)
            .WithMany(s => s.Vorhersagen)
            .HasForeignKey(v => v.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        // "BerechnetAm muss vor Anpfiff erfolgen" (Aufgabenstellung/PREDICTION_FACTORS.md) laesst
        // sich als reiner Postgres-CHECK-Constraint NICHT abbilden, da ein CHECK nur Spalten der
        // eigenen Zeile sehen kann, nicht Spiel.Anstoss einer anderen Tabelle - Postgres kennt
        // keine Cross-Table-CHECKs. Durchsetzung erfolgt daher auf Anwendungsebene beim Erzeugen
        // einer Vorhersage (Vorhersage-Berechnungsjob lehnt BerechnetAm >= Spiel.Anstoss ab) -
        // siehe Ruecksprache-Punkt im Abschlussbericht.
        builder.HasIndex(v => new { v.SpielId, v.BerechnetAm });

        // Genau eine "aktuelle" Vorhersage pro Spiel - partieller Unique-Index nur ueber
        // IstAktuell=true, damit historische Neuberechnungen (IstAktuell=false) nicht kollidieren.
        builder.HasIndex(v => v.SpielId)
            .IsUnique()
            .HasFilter("\"ist_aktuell\" = true")
            .HasDatabaseName("ix_vorhersagen_spiel_id_aktuell");

        // Trefferquote-Aggregation liest gezielt beendete+ausgewertete Vorhersagen.
        builder.HasIndex(v => new { v.IstAktuell, v.TrafExakt, v.TrafTendenz });
    }
}

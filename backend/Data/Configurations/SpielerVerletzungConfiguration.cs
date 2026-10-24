using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class SpielerVerletzungConfiguration : IEntityTypeConfiguration<SpielerVerletzung>
{
    public void Configure(EntityTypeBuilder<SpielerVerletzung> builder)
    {
        builder.ToTable("spieler_verletzungen");

        builder.Property(v => v.Art).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Grund).HasMaxLength(200);

        builder.HasOne(v => v.Spieler)
            .WithMany(s => s.Verletzungen)
            .HasForeignKey(v => v.SpielerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.AusloesendesSpiel)
            .WithMany(s => s.BetroffeneVerletzungen)
            .HasForeignKey(v => v.AusloesendesSpielId)
            .OnDelete(DeleteBehavior.SetNull);

        // Kernabfrage fuer die Vorhersage-Faktoren: "ist Spieler X am Datum Y verletzt/gesperrt?"
        builder.HasIndex(v => new { v.SpielerId, v.Von, v.Bis });

        builder.ToTable(t => t.HasCheckConstraint("ck_verletzung_zeitraum", "\"bis\" IS NULL OR \"bis\" >= \"von\""));
    }
}

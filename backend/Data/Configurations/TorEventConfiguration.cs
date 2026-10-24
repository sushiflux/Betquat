using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class TorEventConfiguration : IEntityTypeConfiguration<TorEvent>
{
    public void Configure(EntityTypeBuilder<TorEvent> builder)
    {
        builder.ToTable("tor_events");

        builder.Property(t => t.SpielerNameFreitext).HasMaxLength(150);

        builder.HasOne(t => t.Spiel)
            .WithMany(s => s.TorEvents)
            .HasForeignKey(t => t.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Spieler)
            .WithMany()
            .HasForeignKey(t => t.SpielerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Haeufigster Zugriff: alle Tore eines Spiels chronologisch (Spiel-Detail-Ansicht).
        builder.HasIndex(t => new { t.SpielId, t.Minute });

        // Basis fuer die 15-Minuten-Segment-Auswertung (PREDICTION_FACTORS.md Abschnitt 10) -
        // Aggregation "Tor-Anteil pro Segment je Team" laeuft ueber Team+Minute.
        builder.HasIndex(t => new { t.TeamId, t.Minute });
    }
}

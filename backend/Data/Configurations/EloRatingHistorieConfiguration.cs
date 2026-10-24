using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class EloRatingHistorieConfiguration : IEntityTypeConfiguration<EloRatingHistorie>
{
    public void Configure(EntityTypeBuilder<EloRatingHistorie> builder)
    {
        builder.ToTable("elo_rating_historie");

        builder.Property(e => e.EloVorSpiel).HasPrecision(8, 2);
        builder.Property(e => e.EloNachSpiel).HasPrecision(8, 2);

        builder.HasOne(e => e.Team)
            .WithMany(t => t.EloHistorie)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Spiel)
            .WithMany()
            .HasForeignKey(e => e.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ein Team hat pro Spiel genau einen Elo-Eintrag.
        builder.HasIndex(e => new { e.TeamId, e.SpielId }).IsUnique();

        // Kernabfrage: "aktuellster Elo-Wert eines Teams vor Datum X" bzw. Zeitreihen-Chart -
        // chronologisch pro Team.
        builder.HasIndex(e => new { e.TeamId, e.SpielZeitpunkt });
    }
}

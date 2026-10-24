using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class XgStatistikConfiguration : IEntityTypeConfiguration<XgStatistik>
{
    public void Configure(EntityTypeBuilder<XgStatistik> builder)
    {
        builder.ToTable("xg_statistiken");

        builder.Property(x => x.Xg).HasPrecision(5, 2);
        builder.Property(x => x.Xga).HasPrecision(5, 2);
        builder.Property(x => x.Xgot).HasPrecision(5, 2);

        builder.HasOne(x => x.Spiel)
            .WithMany(s => s.XgStatistiken)
            .HasForeignKey(x => x.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Datenquelle)
            .WithMany()
            .HasForeignKey(x => x.DatenquelleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Pro Spiel+Team+Quelle genau ein xG-Datensatz (z.B. getrennte Zeilen fuer Understat vs.
        // FBref moeglich - bis zu zwei Zeilen pro Spiel und Team, IstHeimteam ist ein reines
        // Anzeige-/Filter-Flag ohne eigene 1:1-Navigation, siehe Spiel.XgStatistiken).
        builder.HasIndex(x => new { x.SpielId, x.TeamId, x.DatenquelleId }).IsUnique();
    }
}

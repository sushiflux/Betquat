using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class ZeitsegmentStatistikConfiguration : IEntityTypeConfiguration<ZeitsegmentStatistik>
{
    public void Configure(EntityTypeBuilder<ZeitsegmentStatistik> builder)
    {
        builder.ToTable("zeitsegment_statistiken");

        builder.Property(z => z.BallbesitzProzent).HasPrecision(5, 2);

        builder.HasOne(z => z.Spiel)
            .WithMany(s => s.Zeitsegmente)
            .HasForeignKey(z => z.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(z => z.Team)
            .WithMany()
            .HasForeignKey(z => z.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.Datenquelle)
            .WithMany()
            .HasForeignKey(z => z.DatenquelleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(z => new { z.SpielId, z.TeamId, z.SegmentStart, z.DatenquelleId }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_zeitsegment_start_gueltig",
            "\"segment_start\" IN (0, 15, 30, 45, 60, 75)"));
    }
}

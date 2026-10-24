using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class SchiedsrichterConfiguration : IEntityTypeConfiguration<Schiedsrichter>
{
    public void Configure(EntityTypeBuilder<Schiedsrichter> builder)
    {
        builder.ToTable("schiedsrichter");

        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.OpenLigaDbSchiedsrichterId)
            .IsUnique()
            .HasFilter("\"open_liga_db_schiedsrichter_id\" IS NOT NULL");
    }
}

public class SchiedsrichterStatistikConfiguration : IEntityTypeConfiguration<SchiedsrichterStatistik>
{
    public void Configure(EntityTypeBuilder<SchiedsrichterStatistik> builder)
    {
        builder.ToTable("schiedsrichter_statistiken");

        builder.Property(s => s.GelbeKartenSchnitt).HasPrecision(5, 2);
        builder.Property(s => s.GelbRoteKartenSchnitt).HasPrecision(5, 2);
        builder.Property(s => s.RoteKartenSchnitt).HasPrecision(5, 2);
        builder.Property(s => s.ElfmeterSchnitt).HasPrecision(5, 2);

        builder.HasOne(s => s.Schiedsrichter)
            .WithMany(sr => sr.Statistiken)
            .HasForeignKey(s => s.SchiedsrichterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Wettbewerb)
            .WithMany()
            .HasForeignKey(s => s.WettbewerbId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Saison)
            .WithMany()
            .HasForeignKey(s => s.SaisonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Eine aggregierte Statistik-Zeile pro Schiedsrichter+Wettbewerb+Saison - der
        // Batch-Refresh macht ein Upsert gegen genau diesen Schluessel.
        builder.HasIndex(s => new { s.SchiedsrichterId, s.WettbewerbId, s.SaisonId }).IsUnique();
    }
}

using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class SpielerConfiguration : IEntityTypeConfiguration<Spieler>
{
    public void Configure(EntityTypeBuilder<Spieler> builder)
    {
        builder.ToTable("spieler");

        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Position).HasMaxLength(30);

        builder.HasOne(s => s.Team)
            .WithMany(t => t.Spieler)
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.TeamId, s.Name });
    }
}

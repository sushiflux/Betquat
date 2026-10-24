using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class RadioLinkConfiguration : IEntityTypeConfiguration<RadioLink>
{
    public void Configure(EntityTypeBuilder<RadioLink> builder)
    {
        builder.ToTable("radio_links");

        builder.Property(r => r.EinzelspielUrl).HasMaxLength(500);
        builder.Property(r => r.KonferenzUrl).HasMaxLength(500);

        builder.HasOne(r => r.Spiel)
            .WithOne(s => s.RadioLink)
            .HasForeignKey<RadioLink>(r => r.SpielId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SpielId).IsUnique();
    }
}

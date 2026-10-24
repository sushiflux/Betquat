using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class SaisonConfiguration : IEntityTypeConfiguration<Saison>
{
    public void Configure(EntityTypeBuilder<Saison> builder)
    {
        builder.ToTable("saisons");

        builder.Property(s => s.Name).HasMaxLength(20).IsRequired();
        builder.Property(s => s.OpenLigaDbSaisonKuerzel).HasMaxLength(20);

        builder.HasOne(s => s.Wettbewerb)
            .WithMany(w => w.Saisons)
            .HasForeignKey(s => s.WettbewerbId)
            .OnDelete(DeleteBehavior.Restrict);

        // Eine Saison-Bezeichnung ist pro Wettbewerb eindeutig (aber nicht global - "2025/26"
        // existiert fuer alle 4 Wettbewerbe parallel).
        builder.HasIndex(s => new { s.WettbewerbId, s.Name }).IsUnique();

        // Haeufigste Abfrage: "aktuelle Saison eines Wettbewerbs" fuer den Wochen-Feed-Default.
        builder.HasIndex(s => new { s.WettbewerbId, s.IstAktuell });
    }
}

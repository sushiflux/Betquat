using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class TeamAliasConfiguration : IEntityTypeConfiguration<TeamAlias>
{
    public void Configure(EntityTypeBuilder<TeamAlias> builder)
    {
        builder.ToTable("team_aliase");

        builder.Property(a => a.AliasName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.ExterneId).HasMaxLength(100);

        builder.HasOne(a => a.Team)
            .WithMany(t => t.Aliase)
            .HasForeignKey(a => a.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Datenquelle)
            .WithMany()
            .HasForeignKey(a => a.DatenquelleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Kernabfrage des Team-Mappings (PREDICTION_FACTORS.md "Team-/Spiel-Mapping"): beim
        // Import wird nach (Quelle, exakter Alias-Schreibweise) gesucht - muss eindeutig sein,
        // sonst waere das Mapping nicht deterministisch.
        builder.HasIndex(a => new { a.DatenquelleId, a.AliasName }).IsUnique();
    }
}

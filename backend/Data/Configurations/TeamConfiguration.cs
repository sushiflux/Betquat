using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Kurzname).HasMaxLength(50);
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.StandortStadt).HasMaxLength(100);
        builder.Property(t => t.StandortBreitengrad).HasPrecision(9, 6);
        builder.Property(t => t.StandortLaengengrad).HasPrecision(9, 6);

        builder.HasIndex(t => t.Name).IsUnique();

        // OpenLigaDB-Team-Id ist die primaere Re-Import-Referenz - eindeutig, aber nullable
        // (Teams koennen z.B. aus dem Kaggle-Historienimport ohne OpenLigaDB-Id entstehen), daher
        // ein filtered/partial-artiger Unique-Index ueber die Fluent-API-Filterbedingung.
        builder.HasIndex(t => t.OpenLigaDbTeamId)
            .IsUnique()
            .HasFilter("\"open_liga_db_team_id\" IS NOT NULL");
    }
}

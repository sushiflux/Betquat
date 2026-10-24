using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class SpielConfiguration : IEntityTypeConfiguration<Spiel>
{
    public void Configure(EntityTypeBuilder<Spiel> builder)
    {
        builder.ToTable("spiele");

        builder.Property(s => s.SpieltagName).HasMaxLength(100);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.Wettbewerb)
            .WithMany()
            .HasForeignKey(s => s.WettbewerbId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Saison)
            .WithMany(sa => sa.Spiele)
            .HasForeignKey(s => s.SaisonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.HeimTeam)
            .WithMany()
            .HasForeignKey(s => s.HeimTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.AuswaertsTeam)
            .WithMany()
            .HasForeignKey(s => s.AuswaertsTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Schiedsrichter)
            .WithMany(sr => sr.Spiele)
            .HasForeignKey(s => s.SchiedsrichterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Kernindex fuer den Wochen-Feed (APP_STRUCTURE.md Abschnitt 1): "alle Spiele einer KW
        // ueber alle 4 Wettbewerbe, chronologisch" - genau dieser Zugriffspfad.
        builder.HasIndex(s => new { s.KwJahr, s.KwNummer, s.Anstoss });

        // Zweithaeufigster Zugriffspfad: Spieltag-Ansicht/Trefferquote-Aggregation pro
        // Wettbewerb+Saison+Spieltag (TrefferquoteEintrag-Berechnung).
        builder.HasIndex(s => new { s.WettbewerbId, s.SaisonId, s.SpieltagNummer });

        // Live-Polling-Job fragt gezielt "alle aktuell laufenden Spiele" ab.
        builder.HasIndex(s => s.Status);

        builder.HasIndex(s => s.OpenLigaDbMatchId)
            .IsUnique()
            .HasFilter("\"open_liga_db_match_id\" IS NOT NULL");

        builder.HasIndex(s => s.FootballDataOrgMatchId)
            .IsUnique()
            .HasFilter("\"football_data_org_match_id\" IS NOT NULL");

        // Datenintegritaet: ein Team kann nicht gegen sich selbst antreten.
        builder.ToTable(t => t.HasCheckConstraint("ck_spiel_heim_ungleich_auswaerts", "\"heim_team_id\" <> \"auswaerts_team_id\""));

        // Torstaende sind entweder beide gesetzt oder beide null (kein Halbtreffer moeglich) und nie negativ.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_spiel_tore_konsistent",
            "((\"tore_heim\" IS NULL AND \"tore_auswaerts\" IS NULL) OR (\"tore_heim\" IS NOT NULL AND \"tore_auswaerts\" IS NOT NULL)) " +
            "AND (\"tore_heim\" IS NULL OR \"tore_heim\" >= 0) AND (\"tore_auswaerts\" IS NULL OR \"tore_auswaerts\" >= 0)"));
    }
}

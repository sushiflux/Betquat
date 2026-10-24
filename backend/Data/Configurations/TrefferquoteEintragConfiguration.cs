using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetQuote.Api.Data.Configurations;

public class TrefferquoteEintragConfiguration : IEntityTypeConfiguration<TrefferquoteEintrag>
{
    public void Configure(EntityTypeBuilder<TrefferquoteEintrag> builder)
    {
        builder.ToTable("trefferquote_eintraege");

        builder.Property(t => t.Ebene).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.ExakteTrefferquote).HasPrecision(6, 5);
        builder.Property(t => t.TendenzTrefferquote).HasPrecision(6, 5);

        builder.HasOne(t => t.Wettbewerb)
            .WithMany()
            .HasForeignKey(t => t.WettbewerbId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Saison)
            .WithMany()
            .HasForeignKey(t => t.SaisonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Eine Zeile pro (Wettbewerb, Ebene, Saison, Spieltag) - der Batch-Refresh macht ein
        // Upsert gegen genau diesen Schluessel. NULLS im Unique-Index sind in Postgres per Default
        // "distinct" (mehrere Zeilen mit gleichem WettbewerbId+Ebene+SaisonId=NULL+SpieltagNummer=NULL
        // waeren technisch erlaubt) - fuer die Gesamt-Ebene (SaisonId=NULL, SpieltagNummer=NULL)
        // gibt es aber ohnehin nur eine gewollte Zeile pro Wettbewerb, abgesichert zusaetzlich per
        // Anwendungslogik im Batch-Job (immer erst loeschen/upserten anhand WettbewerbId+Ebene,
        // bei Ebene=Gesamt ohne weitere Schluesselteile).
        builder.HasIndex(t => new { t.WettbewerbId, t.Ebene, t.SaisonId, t.SpieltagNummer }).IsUnique();

        // Direkter Lookup-Pfad des Statistik-Widgets: "Gesamt-Zeile pro Wettbewerb" bzw.
        // "alle Spieltag-Zeilen einer Saison" beim Aufklappen der Drill-down-Hierarchie.
        builder.HasIndex(t => new { t.WettbewerbId, t.Ebene });
    }
}

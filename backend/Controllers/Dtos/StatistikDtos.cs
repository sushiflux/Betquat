namespace BetQuote.Api.Controllers.Dtos;

/// <summary>Statistik-Widget (APP_STRUCTURE.md Abschnitt 3) - Trefferquote pro Wettbewerb, mit Drill-down Gesamt-&gt;Saison-&gt;Spieltag.</summary>
public record TrefferquoteZeileDto(
    string Ebene,
    int? SaisonId,
    string? SaisonName,
    int? SpieltagNummer,
    int AnzahlSpiele,
    int AnzahlExakteTreffer,
    decimal ExakteTrefferquote,
    int AnzahlTendenzTreffer,
    decimal TendenzTrefferquote,
    DateTimeOffset BerechnetAm);

public record StatistikWettbewerbDto(int WettbewerbId, string WettbewerbName, TrefferquoteZeileDto? Gesamt, List<TrefferquoteZeileDto> Saisons);

public record StatistikDrilldownDto(int WettbewerbId, int SaisonId, List<TrefferquoteZeileDto> Spieltage);

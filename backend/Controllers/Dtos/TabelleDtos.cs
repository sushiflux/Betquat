namespace BetQuote.Api.Controllers.Dtos;

/// <summary>Klassische Liga-Tabelle, "volle" Ansicht (APP_STRUCTURE.md Abschnitt 1b) - Platz/Team/Spiele/Siege/Unentschieden/Niederlagen/Tore/Gegentore/Tordifferenz/Punkte/Form/Trend.</summary>
public record TabellenZeileDto(
    int Platz,
    int TeamId,
    string TeamName,
    string? TeamKurzname,
    string? TeamLogoUrl,
    int Spiele,
    int Siege,
    int Unentschieden,
    int Niederlagen,
    int ToreGeschossen,
    int ToreKassiert,
    int Tordifferenz,
    int Punkte,
    /// <summary>Form der letzten 5 Spiele dieser Saison, "S"/"U"/"N", chronologisch aeltestes zuerst (so laesst sich die Symbolreihe im Frontend direkt in Anzeigereihenfolge rendern). Kann kuerzer als 5 sein, wenn das Team noch nicht 5 Spiele absolviert hat.</summary>
    List<string> FormLetzte5,
    /// <summary>Tabellenplatz dieses Teams NACH dem vorletzten abgeschlossenen Spieltag (d.h. vor dem zuletzt gespielten Spieltag) - Basis fuer die Trend-Anzeige (APP_STRUCTURE.md Abschnitt 1b: "3. (5.)"). Null wenn es noch keinen "vorherigen" Stand gibt (erster Spieltag der Saison, oder Team hatte damals noch keine Tabellen-relevanten Spiele).</summary>
    int? VorherigerPlatz,
    /// <summary>True wenn dieses Team der Titelverteidiger dieser Liga ist (Meister der letzten abgeschlossenen Saison) - Meisterschale-Icon (APP_STRUCTURE.md Abschnitt 1b "Auszeichnungs-Icons").</summary>
    bool IstTitelverteidiger,
    /// <summary>True wenn dieses Team der aktuelle DFB-Pokalsieger ist - nur fuer 1./2. Bundesliga befuellt, bei La Liga immer false (APP_STRUCTURE.md Abschnitt 1b "Auszeichnungs-Icons").</summary>
    bool IstPokalsieger,
    /// <summary>True wenn dieses Team in der aktuellen Saison aus der jeweils niedrigeren Liga aufgestiegen ist - nur fuer 1./2. Bundesliga befuellt, bei La Liga immer false (APP_STRUCTURE.md Abschnitt 1b "Auszeichnungs-Icons": Segunda Division nicht im Scope).</summary>
    bool IstAufsteiger);

public record LigaTabelleDto(int WettbewerbId, string WettbewerbName, bool IstPokal, List<TabellenZeileDto> Tabelle);

/// <summary>Fuer den DFB-Pokal statt Tabelle: kompakte Rundenuebersicht (APP_STRUCTURE.md: "DFB-Pokal ggf. als Rundenuebersicht statt Tabelle").</summary>
public record PokalRundeDto(string RundenName, int RundenNummer, List<SpielListeDto> Spiele);

public record PokalUebersichtDto(int WettbewerbId, string WettbewerbName, List<PokalRundeDto> Runden);

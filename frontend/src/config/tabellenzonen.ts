/**
 * Farbliche Zonen-Kennzeichnung der Live-Tabelle nach Tabellenplatz (APP_STRUCTURE.md Abschnitt
 * 1b: "Meister-/Aufstiegsplaetze, Champions-League-Plaetze, ..., Abstiegsplaetze,
 * Abstiegsrelegation"). Eine Zone pro (WettbewerbId, Platzbereich) - je Liga eigene Grenzen, da
 * sich die Startplatz-Kontingente unterscheiden.
 *
 * WICHTIG (wie in der Aufgabenstellung explizit verlangt): Diese Platzierungsgrenzen sind ein
 * grober Merkposten aus der aktuellen Wettbewerbslogik (Stand der Aufgabenstellung, nicht durch
 * eine Live-Quelle fuer die Saison 2026/27 verifiziert - in dieser Session stand kein
 * WebSearch-Werkzeug zur Verifikation zur Verfuegung). Die Grundstruktur (1./2. Bundesliga exakt
 * 18 Teams, La Liga 20 Teams; DFL vergibt seit Jahren konstant 4 CL- + 1 EL- + 1 Conference-
 * League-Platz aus der Bundesliga, RFEF/LaLiga vergibt seit der UEFA-Reform 2024 vier CL-Plaetze)
 * ist seit mehreren Spielzeiten stabil, kann sich aber durch die UEFA-Koeffizienten-Rangliste
 * (5-Jahres-Wertung, jaehrlich neu berechnet) leicht verschieben. SOLLTE VOR SAISONENDE 2026/27
 * NOCHMAL GEGEN EINE AKTUELLE QUELLE (z.B. DFL/RFEF/UEFA-Verlautbarung) VERIFIZIERT WERDEN, bevor
 * die Tabelle in einer Abstiegs-/Europapokal-relevanten Schlussphase der Saison angezeigt wird.
 *
 * KORREKTUR (Nutzer hat einen Fehler in einer frueheren Fassung zu Recht hinterfragt): Relegation
 * und direkter Abstieg/Aufstieg sind sportlich unterschiedliche Bedeutungen und duerfen NIE
 * dieselbe Farbe teilen, auch wenn sie auf benachbarten Plaetzen liegen (z.B. Platz 16 der
 * 2. Bundesliga = Relegation gegen die 3. Liga, Platz 17-18 = direkter Abstieg - zwei eigene
 * Zonentypen/Farben, nicht eine gemeinsame "untere Zone"). Jeder der sieben Zonentypen unten hat
 * daher eine eigene, optisch klar unterscheidbare Farbe.
 */

export type TabellenZonenTyp =
  | 'meister'
  | 'championsLeague'
  | 'europaLeague'
  | 'conferenceLeague'
  | 'relegationAufstieg'
  | 'relegationAbstieg'
  | 'abstieg'

export interface TabellenZone {
  typ: TabellenZonenTyp
  vonPlatz: number
  bisPlatz: number
  label: string
}

/** WettbewerbId -> Zonen-Liste, aufsteigend nach vonPlatz. Reihenfolge relevant fuer findZone (erste passende Zone gewinnt bei Ueberlappung, z.B. Platz 1 = Meister UND Champions League -> Meister gewinnt als spezifischere Zone). */
const ZONEN: Record<number, TabellenZone[]> = {
  // 1. Bundesliga (18 Teams)
  1: [
    { typ: 'meister', vonPlatz: 1, bisPlatz: 1, label: 'Meisterschaft' },
    { typ: 'championsLeague', vonPlatz: 1, bisPlatz: 4, label: 'Champions League' },
    { typ: 'europaLeague', vonPlatz: 5, bisPlatz: 5, label: 'Europa League' },
    { typ: 'conferenceLeague', vonPlatz: 6, bisPlatz: 6, label: 'Conference-League-Qualifikation' },
    // Platz 16 = Relegation gegen Platz 3 der 2. Liga - eigene Zone, NICHT "Abstieg".
    { typ: 'relegationAbstieg', vonPlatz: 16, bisPlatz: 16, label: 'Relegation (Abstieg)' },
    { typ: 'abstieg', vonPlatz: 17, bisPlatz: 18, label: 'Direkter Abstieg' },
  ],
  // 2. Bundesliga (18 Teams)
  2: [
    { typ: 'championsLeague', vonPlatz: 1, bisPlatz: 2, label: 'Direkter Aufstieg' },
    // Platz 3 = Relegation gegen Platz 16 der 1. Liga - eigene Zone, NICHT "Aufstieg".
    { typ: 'relegationAufstieg', vonPlatz: 3, bisPlatz: 3, label: 'Relegation (Aufstieg)' },
    // Platz 16 = Relegation gegen die 3. Liga - eigene Zone, NICHT "Abstieg" (auch wenn der
    // eigentliche Play-off-Gegner aus der 3. Liga kommt, die wir nicht mehr aktiv importieren -
    // die Zone selbst bleibt trotzdem farblich von 17/18 unterschieden).
    { typ: 'relegationAbstieg', vonPlatz: 16, bisPlatz: 16, label: 'Relegation (Abstieg)' },
    { typ: 'abstieg', vonPlatz: 17, bisPlatz: 18, label: 'Direkter Abstieg' },
  ],
  // La Liga (20 Teams) - keine Relegation zwischen 1./2. Liga, anders als die deutschen Ligen.
  5: [
    { typ: 'meister', vonPlatz: 1, bisPlatz: 1, label: 'Meisterschaft' },
    { typ: 'championsLeague', vonPlatz: 1, bisPlatz: 4, label: 'Champions League' },
    { typ: 'europaLeague', vonPlatz: 5, bisPlatz: 6, label: 'Europa League' },
    { typ: 'conferenceLeague', vonPlatz: 7, bisPlatz: 7, label: 'Conference League' },
    { typ: 'abstieg', vonPlatz: 18, bisPlatz: 20, label: 'Direkter Abstieg' },
  ],
}

/** Liefert die Zone fuer einen Platz in einem Wettbewerb, falls vorhanden. Bei Ueberlappung (z.B. Platz 1 = Meister UND Champions League) gewinnt die spezifischere/erste passende Zone der Liste. */
export function findZone(wettbewerbId: number, platz: number): TabellenZone | null {
  const zonen = ZONEN[wettbewerbId]
  if (!zonen) return null
  return zonen.find((z) => platz >= z.vonPlatz && platz <= z.bisPlatz) ?? null
}

/** Alle Zonen einer Liga in Anzeigereihenfolge, fuer die Legende unterhalb der Tabelle. */
export function zonenFuerLiga(wettbewerbId: number): TabellenZone[] {
  return ZONEN[wettbewerbId] ?? []
}

/**
 * Zeilen-Hintergrund + linker Akzent-Rand je Zonentyp (Nutzer-Praezisierung: "auf die komplette
 * Tabellenzeile angewendet, nicht nur eine einzelne Spalte/den Rand"). Eigene Akzentfarben-
 * Zuordnung (siehe CLAUDE.md "Action-Farbkategorien"), unabhaengig von den bestehenden
 * semantischen success/danger-Tokens, da eine Tabellenzone eine andere Bedeutungskategorie ist
 * als ein Treffer/Fehltreffer. Hintergrund bewusst als dezente ~10% Deckkraft ueber der Akzent-
 * farbe gehalten (WCAG-Textkontrast bleibt durch die ohnehin helle/dunkle Text-Farbe unberuehrt,
 * da nur der Hintergrund leicht eingefaerbt wird, nicht der Text selbst). Jeder Zonentyp hat eine
 * eigene, von den Nachbar-Zonen klar unterscheidbare Farbe (siehe Datei-Kommentar oben: Relegation
 * und direkter Auf-/Abstieg teilen sich NIE dieselbe Farbe).
 */
const ZONE_FARBEN: Record<TabellenZonenTyp, string> = {
  meister: '#c9a227',
  championsLeague: '#5b93f5',
  europaLeague: '#e07a1f',
  conferenceLeague: '#1f9d8f',
  relegationAufstieg: '#8a5cf6',
  relegationAbstieg: '#c9832a',
  abstieg: '#e0524f',
}

// Klassen als statische Literale (nicht aus ZONE_FARBEN zur Laufzeit zusammengesetzt!) - Tailwind
// 4 generiert Utility-Klassen per JIT-Scan des Quelltexts, dynamisch per Template-String gebaute
// Klassennamen wie `border-l-[${farbe}]` wuerden vom Scanner nicht erkannt und still wegfallen.
const ZONE_STYLES: Record<TabellenZonenTyp, string> = {
  meister: 'border-l-[3px] border-l-[#c9a227] bg-[#c9a227]/10',
  championsLeague: 'border-l-[3px] border-l-[#5b93f5] bg-[#5b93f5]/10',
  europaLeague: 'border-l-[3px] border-l-[#e07a1f] bg-[#e07a1f]/10',
  conferenceLeague: 'border-l-[3px] border-l-[#1f9d8f] bg-[#1f9d8f]/10',
  relegationAufstieg: 'border-l-[3px] border-l-[#8a5cf6] bg-[#8a5cf6]/10',
  relegationAbstieg: 'border-l-[3px] border-l-[#c9832a] bg-[#c9832a]/10',
  abstieg: 'border-l-[3px] border-l-[#e0524f] bg-[#e0524f]/10',
}

/** Klassen fuer die komplette Tabellenzeile (Hintergrund + linker Rand). Faellt auf einen transparenten Rand zurueck, wenn der Platz zu keiner Zone gehoert - der normale Zeilen-Hintergrund (bg-bg-inset/60) kommt dann separat vom aufrufenden Code. */
export function zoneRowClass(zone: TabellenZone | null): string {
  return zone ? ZONE_STYLES[zone.typ] : 'border-l-[3px] border-l-transparent'
}

/** Farbwert (Hex) einer Zone, fuer die Legende (kleines Farbkaestchen). */
export function zoneColor(typ: TabellenZonenTyp): string {
  return ZONE_FARBEN[typ]
}

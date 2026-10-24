# BetQuote – Seitenaufbau & UI-Struktur

Diese Datei beschreibt, wie die App aufgebaut sein soll: Seiten, Navigation, Inhalte pro Ansicht.
Kein Code, keine konkrete Optik/Styling (das ist Aufgabe des `designer`-Agents, sobald wir ins
Umsetzen gehen) — hier geht es um die **Informationsarchitektur**: was gibt es, wo, in welcher
Beziehung zueinander. Fachliche Grundlage siehe [PREDICTION_FACTORS.md](PREDICTION_FACTORS.md).

---

## Grundprinzip

**Erneut überarbeitet nach Nutzer-Feedback** — ursprünglich als reiner "Spieltag-Feed" mit
separaten Unterseiten geplant, dann zu einem global gemischten Wochen-Feed vereinfacht; jetzt
final zu einer **Tab-Navigation nach Land → Liga** präzisiert (siehe Git-Historie dieser Datei für
die Zwischenfassungen, falls relevant). Weiterhin eine Single-Page-App ohne klassische
Mehrseiten-Navigation, aber **nicht mehr alle Wettbewerbe global gemischt** — stattdessen:

```
[ 🇩🇪 Deutschland ]   [ 🇪🇸 Spanien ]        ← Land-Tabs (oberste Ebene)

  [ 1. Bundesliga ]   [ 2. Bundesliga ]     ← Liga-Unter-Tabs (nur bei Deutschland-Tab)
```

Bei "Spanien" gibt es aktuell nur einen Unter-Tab (La Liga) — Struktur ist aber bewusst so
angelegt, dass später weitere spanische Liga-Ebenen (z. B. Segunda División) einfach als weiterer
Unter-Tab ergänzt werden könnten, ohne die Navigation umzubauen. Genauso ließe sich später ein
weiteres Land als dritter oberster Tab ergänzen.

**Innerhalb jedes gewählten Liga-Tabs** (z. B. "1. Bundesliga") zeigt die Ansicht dann genau das,
was vorher als global gemischter Feed geplant war — nur eben gefiltert auf diese eine Liga statt
alle drei gemischt: den **Wochen-Feed** dieser Liga (siehe Abschnitt 1) organisiert **nach
Kalenderwoche statt nach Spieltag** (siehe dort für die Begründung), mit Navigation in beide
Zeitrichtungen (vergangene UND zukünftige Wochen), sowie daneben die **Live-Tabelle** genau dieser
einen Liga (kein Umschalt-Dropdown mehr nötig wie in der vorherigen Fassung — die Liga-Auswahl
passiert jetzt schon über den Tab selbst). Jedes Spiel zeigt direkt in der Übersicht sowohl den
echten/Live-Stand als auch unsere Vorhersage — kein Klick nötig, um den Haupttipp zu sehen.

**Layout-Breite** (vom Nutzer explizit gewünscht, Korrektur ggü. der ersten Umsetzung): Die Ansicht
nutzt **immer die komplette Breite des Browserfensters**, keine feste/maximale Container-Breite,
die auf breiten Bildschirmen ungenutzten Leerraum an den Seiten lässt. Innerhalb dieser vollen
Breite ist die **Spaltenverteilung zwischen Wochen-Feed und Live-Tabelle anzupassen**: der
Wochen-Feed (Spiel-Karten) darf schmaler sein, die Live-Tabelle braucht dafür mehr Platz, seit sie
auf die volle Spaltenzahl (Platz/Team/Spiele/S/U/N/Tore/Gegentore/Tordifferenz/Punkte/Form)
erweitert wurde (siehe Abschnitt 1b) — Ziel ist, dass die Tabelle **ohne horizontales Scrollen**
lesbar bleibt, auf einem normalen Desktop-Bildschirm.

**Datenimport-Hinweis** (vom Nutzer explizit klargestellt): Für jede Liga wird die **komplette
laufende Saison ab dem 1. Spieltag** importiert, nicht erst ab dem heutigen Datum — das sorgt für
korrekte Tabellenstände, Formkurven und Head-to-Head-Historie von Saisonbeginn an. Für Spieltage,
die bereits **vor dem Zeitpunkt liegen, an dem wir mit dem Tippen begonnen haben**, gibt es
logischerweise keine rückwirkend erstellte Vorhersage (die kann nur für Spiele ab jetzt berechnet
werden, siehe PREDICTION_FACTORS.md "Vorhersage muss vor Anpfiff erfolgen") — solche älteren Spiele
zeigen also nur das echte Ergebnis, ohne Tipp-Vergleich, während der aktuelle Tabellenstand davon
unberührt korrekt ist.

## Datenbasis pro Spiel (was wir überhaupt anzeigen können)

Aus **OpenLigaDB** (unsere Primärquelle, siehe PREDICTION_FACTORS.md) bekommen wir bereits pro
Spiel mitgeliefert, ohne weitere Quelle:
- Team-Namen, Kurznamen, **Team-Logo-URL** (`teamIconUrl`, direkt nutzbar)
- Datum/Uhrzeit, Spieltag-Zuordnung
- Laufender Spielstand während des Spiels (Live-Update, inkl. Halbzeit-/Endergebnis)
- Torschützen mit Minute, Elfmeter-/Eigentor-Kennzeichnung
- `matchIsFinished`-Flag → damit lässt sich sauber zwischen "vergangen", "live" und "kommend"
  unterscheiden

Das deckt "Live-Ergebnis" und "Logos" aus deiner Anfrage direkt ab — keine zusätzliche
Live-Daten-API nötig, das läuft über dieselbe Quelle wie unsere historischen Daten.

---

## Bereiche der App

### 1. Wochen-Feed (Hauptansicht, innerhalb des gewählten Liga-Tabs)

**Gefiltert auf die aktuell im Tab gewählte Liga** (siehe "Grundprinzip" oben — Deutschland/
1. Bundesliga, Deutschland/2. Bundesliga oder Spanien/La Liga), **organisiert nach Kalenderwoche,
nicht nach Spieltag** — vom Nutzer explizit so gewünscht, weil eine Kalenderwoche unabhängig von
der genauen Spieltag-Zählung navigierbar ist und auch bei unterschiedlichen Anstoßterminen
zuverlässig funktioniert. Innerhalb einer KW sind die Spiele nach Wochentag gruppiert.

Da jetzt nur noch eine Liga gleichzeitig sichtbar ist (statt aller drei gemischt), entfällt das
Wettbewerb-Badge auf der Spiel-Karte (siehe Beispiel unten) — welche Liga man sieht, ergibt sich
eindeutig aus dem aktiven Tab.

Struktur von oben nach unten (Beispiel für den Tab "Deutschland → 1. Bundesliga"):

```
[ 🇩🇪 Deutschland ]   [ 🇪🇸 Spanien ]
  [ 1. Bundesliga ]   [ 2. Bundesliga ]

[ Wochen-Navigation: ← KW 20   KW 21   KW 22 → ]

── Freitag, 22.05. ──────────────────────────────────────────────
┌─ Spiel-Karte ─────────────────────────────────────────★ hohe ─┐
│                                                     [📻 Radio] │  Konfidenz
│ [Logo] FC Bayern München    – : –    Borussia Dortmund [Logo] │  (farbliche
│ Anstoß: 20:30 Uhr                                              │  Hervor-
│ Tipp: 2:1  (13 %)  · Tendenz: Heim 48 % · Remis 26 % · Ausw 26%│  hebung)
└─────────────────────────────────────────────────────────────┘

── Samstag, 23.05. ──────────────────────────────────────────────
┌─ Spiel-Karte ────────────────────────────────────────────────┐
│                                                     [📻 Radio] │
│ [Logo] Team A                2 : 1    Team B         [Logo]   │
│ Live 67'  ·  Tipp war: 2:1 ✓ (Treffer!)                        │
└─────────────────────────────────────────────────────────────┘

── Sonntag, 24.05. ──────────────────────────────────────────────
  ...
```

- Nur die Spiele der aktuell im Tab gewählten Liga, gruppiert nach Wochentag (Mo–So) — kein
  Wettbewerb-Badge mehr nötig (siehe oben)
- **Dynamische Sortierung innerhalb eines Tages** (vom Nutzer explizit gewünscht): **aktuell
  laufende Spiele stehen immer oben**, auch wenn sie chronologisch nach einem bereits beendeten
  Spiel des gleichen Tages angesetzt waren. Sobald ein Spiel offiziell beendet ist, rutscht es
  wieder nach unten (Priorität: live > noch nicht angepfiffen (chronologisch) > beendet). Das
  betrifft nur die Sortierung *innerhalb* eines Wochentag-Blocks, die Wochentag-Gruppierung selbst
  bleibt chronologisch (Freitag vor Samstag vor Sonntag)
- **Noch nicht angepfiffene Spiele**: Ergebnis wird als **"– : –"** dargestellt (vom Nutzer
  explizit so gewünscht), daneben unser Tipp inkl. Wahrscheinlichkeit — Vorhersage ist also schon
  vor Anpfiff sichtbar, nicht erst danach
- **Laufende Spiele**: Live-Stand + Minute, unser ursprünglicher Tipp bleibt sichtbar zum Vergleich.
  **Live-Spielzeit-Präzisierung** (vom Nutzer explizit gewünscht): die Minutenanzeige soll die
  **echte laufende Spielzeit inkl. Nachspielzeit** zeigen (z. B. "90+3'" statt nur "90'"). OpenLigaDB
  (unsere Hauptquelle) liefert dafür kein Feld — nur Tor-Zeitpunkte, keine generelle Spieluhr, also
  aktuell auch keine echte Nachspielzeit-Information. Lösung: **Sofascore** (ohnehin schon für
  xGOT/Ballbesitz-Zeitsegmente vorgesehen, siehe PREDICTION_FACTORS.md Abschnitt 10) wird auch für
  die Live-Minute inkl. Nachspielzeit genutzt — bis dieses Modul steht, bleibt die aktuelle,
  vereinfachte Anzeige (aus Anstoßzeit berechnete Näherungsminute ohne Nachspielzeit) als
  Übergangslösung bestehen
- **Fehlender Tor-Highlight-Effekt (offener Punkt, kein Bug im engeren Sinn, aber unvollständig
  umgesetzt)**: In einer früheren Fassung dieser Datei war explizit ein visueller Puls-/
  Highlight-Effekt vorgesehen, der aufblitzt, sobald sich während eines laufenden Spiels der
  Spielstand ändert (siehe Abschnitt 1b für das analoge, bereits umgesetzte Pendant bei
  Tabellen-Platzierungswechseln). In der tatsächlichen Umsetzung fehlt dieser Effekt auf der
  Spiel-Karte — aktuell animiert nur die Umsortierung (live-Spiele nach oben), nicht das
  Ergebnis-Update selbst. Nachzuholen: kurzer farbiger Aufblitz-Effekt auf dem
  Ergebnis-Zahlenblock, sobald sich `toreHeim`/`toreAuswaerts` eines laufenden Spiels ändert
- **Beendete Spiele**: echtes Endergebnis, direkt daneben ob unser Tipp exakt richtig lag
  (visuelle Kennzeichnung ✓/✗ o. ä.), siehe auch Statistik-Abschnitt weiter unten
- **Konfidenz-Markierung** (vom Nutzer gewünscht, siehe PREDICTION_FACTORS.md für die fachliche
  Herleitung und die bewusste Abgrenzung von Value-Betting): Spiele, bei denen unser Modell
  ungewöhnlich sicher ist (Top-Ergebnis/Tendenz deutlich vor den Alternativen), werden auf der
  Karte visuell hervorgehoben (z. B. Rahmenfarbe/kleines Symbol) — reines Signal aus der
  Modell-eigenen Konfidenz heraus, kein Abgleich mit Buchmacher-Quoten, keine Handlungsempfehlung.
  **Positions-Korrektur (Bug, vom Nutzer gemeldet)**: das Badge saß in der ersten Umsetzung oben
  rechts auf der Karte und überdeckte dort das Team-Logo — gehört stattdessen **unten rechts** auf
  die Karte, wo es mit nichts anderem kollidiert
- Navigation zwischen Kalenderwochen in beide Richtungen (zurück in die Historie, vor in
  angesetzte künftige Wochen, soweit terminiert)
- **Radio-Link pro Spiel** (vom Nutzer gewünscht, bewusst statt TV-Sender-Anzeige — TV-Rechte sind
  für dieses Projekt irrelevant): kleines Radio-Icon auf der Spiel-Karte, sofern eine
  Audiostream-Übertragung für dieses Spiel bekannt ist (Einzelspiel-Kommentar und/oder Konferenz).
  Klick öffnet den Audiostream direkt im Browser (neuer Tab), kein eigener In-App-Player nötig.
  Quelle: ARD Sportschau Audiostreams (siehe TECH_STACK.md) — nur für 1./2. Bundesliga verfügbar
  (deutschsprachige Quelle), für La Liga bislang keine deutsche Radioübertragung recherchiert;
  Icon erscheint dann schlicht nicht, kein Fehlerzustand nötig

### 1b. Live-Tabelle (neben/unter dem Wochen-Feed, immer sichtbar, innerhalb des gewählten Liga-Tabs)

**Erweitert nach Nutzer-Feedback auf die "volle" Ansicht** (nicht die vorherige kompakte Fassung):
für die **aktuell im Tab gewählte Liga** (siehe "Grundprinzip"), die sich **live mitaktualisiert**,
sobald sich während eines laufenden Spiels durch ein Tor die Tabellenkonstellation ändert. Gilt für
alle drei Ligen gleichermaßen, da alle drei im klassischen Ligamodus laufen.

**Spalten (volle Ansicht)**: Platz (inkl. Trend-Anzeige, siehe unten), Team (mit Logo), Spiele,
**Siege, Unentschieden, Niederlagen** (einzeln, nicht nur zusammengefasst), **Tore, Gegentore**,
Tordifferenz, Punkte, **Form der letzten 5 Spiele** (als Symbolreihe S/U/N, farblich: Sieg grün,
Niederlage rot, Unentschieden grau).

**Kompaktes Spacing** (vom Nutzer explizit korrigiert — erste Umsetzung war zu breit): möglichst
knappes Padding zwischen den Spalten, kein übertriebener Abstand v. a. zwischen Team-Name und den
folgenden Zahlenspalten — Ziel ist eine insgesamt schmalere, dichtere Tabelle (spielt auch mit der
"volle Fensterbreite ohne horizontales Scrollen"-Vorgabe weiter oben zusammen: je kompakter die
Tabelle, desto eher passt sie ohne Scrollen).

**Trend-Anzeige pro Team** (vom Nutzer explizit gewünscht): neben der Platzierung ein kleines
Dreieck/Pfeil-Symbol, das anzeigt ob sich die Platzierung gegenüber dem letzten Spieltag verbessert
oder verschlechtert hat — grün nach oben bei Verbesserung, rot nach unten bei Verschlechterung
(kein Symbol bzw. neutral/grau bei unveränderter Platzierung). Daneben in Klammern die exakte
Platzierung vom letzten Spieltag (z. B. "3. ▲ (5.)" für "aktuell Platz 3, war zuvor Platz 5").
Braucht historische Tabellenstände (Platzierung nach dem vorherigen Spieltag) als Datengrundlage —
falls das Backend das noch nicht liefert/speichert, ist das ggf. zu ergänzen (aus den bereits
vorhandenen Spieldaten pro Spieltag berechenbar, keine neue externe Quelle nötig).

**Farbliche Zonen-Kennzeichnung** (vom Nutzer explizit gewünscht, wie bei klassischen Liga-Tabellen
üblich — **auf die komplette Tabellenzeile angewendet, nicht nur eine einzelne Spalte/den
Rand** — z. B. dezent eingefärbter Zeilen-Hintergrund je nach Tabellenplatz).

**Korrektur/Präzisierung (Nutzer hat einen Fehler in der vorherigen Fassung dieser Notiz zu Recht
hinterfragt — Platz 16 der 2. Bundesliga hatte fälschlich dieselbe Farbe wie 17/18, dabei ist Platz
16 Relegation, nicht direkter Abstieg — jeweils eine EIGENE Farbe pro Zone, Relegation und direkter
Abstieg dürfen NICHT dieselbe Farbe teilen):**

- **1. Bundesliga** (18 Teams): Platz 1 = Meister, 1–4 = Champions League, 5 = Europa League,
  6 = Conference-League-Qualifikation, **16 = Relegation** (Play-off gegen Platz 3 der 2. Liga —
  eigene Farbe, nicht "Abstieg"), 17–18 = direkter Abstieg
- **2. Bundesliga** (18 Teams): Platz 1–2 = direkter Aufstieg, **3 = Relegation** (Play-off gegen
  Platz 16 der 1. Liga — eigene Farbe, nicht "Aufstieg"), **16 = Relegation-Abstieg** (Play-off
  gegen die 3. Liga — eigene Farbe; da wir die 3. Liga nicht mehr aktiv im Scope haben, kann dieses
  Play-off selbst nicht abgebildet werden, aber Platz 16 muss trotzdem als eigene Zone farblich von
  17/18 unterschieden werden, sonst wirkt die Tabelle so, als gäbe es dort gar keine Unterscheidung
  ), 17–18 = direkter Abstieg
- **La Liga** (20 Teams): Platz 1 = Meister, 1–4 = Champions League, 5–6 = Europa League,
  7 = Conference League, 18–20 = direkter Abstieg (La Liga kennt keine Relegation zwischen 1./2.
  Liga, anders als die deutschen Ligen)

**Jede dieser Zonen (Meister, Champions League, Europa League, Conference-League-Quali, Relegation
[Aufstiegsseite], Relegation [Abstiegsseite], direkter Abstieg) braucht eine optisch klar
unterscheidbare eigene Farbe** — zwei benachbarte Plätze mit unterschiedlicher sportlicher
Bedeutung (z. B. "steigt direkt ab" vs. "spielt nur Relegation") dürfen nie dieselbe Farbe haben,
das war der konkrete Fehler in der ersten Umsetzung. **Diese Zahlen sind weiterhin ein Merkposten
aus der aktuellen Wettbewerbslogik und sollten bei der Umsetzung nochmal gegen die tatsächlichen
Saison-2026/27-Regularien geprüft werden** (Startplatz-Kontingente für Europapokal können sich je
nach UEFA-Koeffizient leicht verschieben) — aber die Grundstruktur (Relegation ≠ direkter Abstieg,
eigene Farbe) ist keine Vermutung, sondern gesichertes Regelwerk der DFL.

**Legende unterhalb der Tabelle** (vom Nutzer explizit ergänzt): erklärt die Bedeutung der
Farbzonen aus dem vorigen Punkt in Textform (z. B. kleine Farbkästchen mit Beschriftung "Champions
League", "Europa League", "Abstieg", "Relegation" usw.) — ohne Legende wären die Farben für sich
allein nicht selbsterklärend.

**Auszeichnungs-Icons neben dem Teamnamen** (vom Nutzer explizit ergänzt), drei Typen:
- **Meisterschale**: beim Team, das die **letzte Meisterschaft dieser Liga** gewonnen hat (z. B. in
  der 1.-Bundesliga-Tabelle bekommt der aktuelle Titelverteidiger das Schale-Symbol)
- **DFB-Pokal-Icon**: beim Team, das den **letzten DFB-Pokal** gewonnen hat — betrifft nur die
  deutschen Ligen (1./2. Bundesliga), da nur dort ein deutscher Pokalsieger sinnvoll zuordenbar
  ist. **Wichtig**: Dafür wird der OpenLigaDB-Import für den DFB-Pokal wieder aktiviert — siehe
  PREDICTION_FACTORS.md "Präzisierung zum DFB-Pokal" für die genaue Abgrenzung: das macht den
  Pokal *nicht* wieder zu einem eigenen Vorhersage-Wettbewerb, es dient ausschließlich dazu, den
  aktuellen Sieger für dieses Icon zu ermitteln
- **"A" für Aufsteiger**: beim Team, das in der aktuellen Saison aus der jeweils niedrigeren Liga
  aufgestiegen ist. **Nur bei 1./2. Bundesliga** (wo beide Ligen im Scope sind und der Aufstieg
  damit zuverlässig bekannt ist) — bei La Liga bewusst weggelassen, da die spanische zweite Liga
  (Segunda División) nicht im Scope ist und wir sonst mit unsicheren/veralteten Daten arbeiten
  müssten

**Animationen** (vom Nutzer explizit gewünscht, `motion`-Bibliothek gemäß Tech-Stack-Vorgabe):
- **Platzierungswechsel**: wenn ein Team durch ein Tor/Ergebnis ein anderes Team in der Tabelle
  überholt, wird das als sichtbare Bewegung animiert (Zeilen tauschen ihre Position mit einer
  sanften Übergangsanimation), nicht nur ein stummer Sprung im DOM
- **Tor-Ereignis**: wenn irgendwo im aktuell laufenden Spiel-Set ein Tor fällt, wird das visuell
  hervorgehoben (z. B. kurzer Highlight-/Puls-Effekt auf der betroffenen Spiel-Karte, Toranimation
  beim Ergebnis-Update) — Ziel ist ein "Live"-Gefühl, nicht nur ein stiller Datenrefresh

**Kein separater Umschalter mehr nötig** (Korrektur ggü. vorheriger Fassung dieser Datei): die
Liga-Auswahl passiert jetzt bereits über die Land-/Liga-Tabs selbst (siehe "Grundprinzip"), nicht
mehr über ein zusätzliches Dropdown an der Tabelle.

### 2. Spiel-Detail (Ausklapp-/Modal-Bereich, keine eigene Seite)

Erreichbar per Klick auf eine Spiel-Karte — öffnet sich innerhalb derselben Seite (Modal/Ausklapp-
Panel, kein Seitenwechsel mit eigener URL nötig, da wir bewusst auf eine Mehrseiten-Navigation
verzichten). Zeigt zusätzlich zur bereits sichtbaren Kompakt-Vorhersage:

- Vollständige Wahrscheinlichkeitsverteilung über alle relevanten Ergebnisse (nicht nur Top 3–4
  wie in der Karte)
- **Begründung/Nachvollziehbarkeit der Vorhersage**: welche Faktoren aus PREDICTION_FACTORS.md
  wie stark eingeflossen sind (z. B. "Bayern: 4 Siege in den letzten 5 Spielen", "Dortmund:
  Stammtorwart verletzt", "Head-to-Head: 3 der letzten 5 Duelle gingen an Bayern") — macht die
  Zahl nachvollziehbar statt einer Blackbox
- Bei beendeten Spielen: Torschützen-Liste mit Minute (aus OpenLigaDB direkt verfügbar)
- Direkter Vergleich Tipp vs. tatsächliches Ergebnis, falls das Spiel schon gelaufen ist
- Head-to-Head-Historie beider Teams (letzte Duelle)
- Formkurve beider Teams (letzte 5–10 Spiele als kompakte Symbolreihe, z. B. S/U/N)

Team-Detailinfos (Name, Logo, Formkurve, Saison-Spielliste) und die klassische Liga-Tabelle sind
**keine eigenständigen Seiten mehr**, sondern in den Wochen-Feed (Abschnitt 1) bzw. das
Live-Tabellen-Widget (Abschnitt 1b) integriert — dort wo sie im Kontext gebraucht werden, statt
als separate Navigation.

### 3. Statistik-Widget ("Wie gut lag ich richtig?")

**Layout-Hinweis vom Nutzer**: da es eine Web-Ansicht ist (nicht mobil-gestapelt), läuft das nicht
untereinander unter dem Wochen-Feed, sondern als **eigenständiges Widget seitlich neben dem
Wochen-Feed** (z. B. rechte Spalte, neben der Live-Tabelle aus Abschnitt 1b) — Breitbild-Layout
mit Hauptbereich + Seitenspalte(n), statt einer langen vertikal gestapelten Seite.

**Korrektur ggü. vorheriger Fassung dieser Datei** (vom Nutzer explizit geändert): Das Widget ist
jetzt **durch den aktiven Land-/Liga-Tab gefiltert**, genau wie Wochen-Feed und Live-Tabelle — ist
man im Tab "1. Bundesliga", zeigt das Widget ausschließlich die Bundesliga-Trefferquote, nicht mehr
liga-übergreifend alle drei gemischt. Wechselt man den Tab, wechselt die Statistik mit.

**Als Kacheln dargestellt** (vom Nutzer explizit gewünscht), eine Kachel pro Kennzahl statt einer
Textwand/Tabelle, z. B.:

```
┌─ Exaktes Ergebnis ─┐ ┌─ Tendenz 1X2 ─────┐ ┌─ Aktuelle Saison ──┐
│                     │ │                    │ │                    │
│       23 %          │ │        58 %        │ │   12 von 34 exakt  │
│  (dieser Spieltag)   │ │  (dieser Spieltag) │ │   21 von 34 Tendenz│
└─────────────────────┘ └────────────────────┘ └────────────────────┘
```

Zeigt die eigene Modell-Performance ehrlich und transparent, damit die im Vorfeld besprochene
realistische Erwartungshaltung (siehe PREDICTION_FACTORS.md) nicht nur Text bleibt, sondern
sichtbar wird:

- **Aktualisierung erst nach Abschluss eines Spieltags** (vom Nutzer explizit so gewünscht — kann
  logischerweise erst berechnet werden, wenn die Ergebnisse feststehen, nicht während laufender
  Spiele)
- **Trefferquote pro Spieltag**: für jeden einzelnen Spieltag der aktiven Liga eine eigene
  Auswertung — wie viele Spiele dieses Spieltags wurden exakt richtig getippt (X von Y, in %), wie
  viele lagen bei der 1X2-Tendenz richtig
- **Trefferquote pro Saison** (vom Nutzer explizit gefordert): dieselbe Auswertung aufsummiert über
  eine komplette Saison der aktiven Liga. Saisonwerte setzen sich aus den Spieltag-Werten zusammen
  (Spieltag → Saison → Gesamt als klare Drill-down-Hierarchie), damit man von der Gesamtquote in
  eine konkrete Saison und von dort in einen konkreten Spieltag springen kann
- Trefferquote exaktes Ergebnis vs. Trefferquote 1X2-Tendenz (auch wenn exaktes Ergebnis nicht
  getroffen wurde: lag die Richtung wenigstens richtig?) — jeweils eigene Kachel
- Optional: Vergleich gegen die Buchmacher-Referenzwerte aus PREDICTION_FACTORS.md Abschnitt 6
  (rein zur Einordnung "sind wir in der Größenordnung der Profis", kein Wettbezug)
- Entwicklung der Trefferquote über die Zeit (verbessert sich das Modell, je mehr Daten/Iterationen
  es gibt? — als Zeitverlauf-Chart über die Spieltag-/Saison-Werte hinweg)

Die Kachel-Grundansicht (aktueller Spieltag + Saison, exakt + Tendenz) reicht vermutlich als
Standardansicht; die volle Drill-down-Ansicht (Spieltag-für-Spieltag-Liste, Zeitverlauf-Chart) kann
bei Bedarf ausklappen/vergrößern, statt das Widget dauerhaft groß zu halten — Detail bei der
Umsetzung durch den `designer`-Agenten zu klären.

---

## Offene Punkte für später

- [ ] Genaues Layout/Styling (Farben, Typografie, Light/Dark-Theme) — Aufgabe des `designer`-Agents,
  sobald wir ins konkrete UI-Bauen gehen; folgt den globalen Default-Vorgaben (siehe CLAUDE.md:
  Tech-Stack React/Vite/Tailwind, WCAG-Kontrast, Border-Radius-Konsistenz etc.)
- [ ] Wie weit im Voraus sind künftige Spieltage überhaupt terminiert/bekannt (abhängig davon, wie
  weit DFL/DFB den Spielplan im Voraus veröffentlichen — i. d. R. mehrere Spieltage im Voraus, aber
  nicht die ganze Saison auf einmal)
- [ ] Mobile-Ansicht der Spiel-Karten (wie viel von der "ausführlichen" Vorhersage passt auf
  schmale Bildschirme, ohne die Liste zu überladen) — inkl. wie das Breitbild-Layout (Wochen-Feed +
  Live-Tabelle + Statistik-Widget nebeneinander, siehe Abschnitte 1/1b/3) auf schmalen Bildschirmen
  gestapelt wird, da es dort primär für die Desktop-/Web-Nutzung konzipiert ist
- [ ] Suchfunktion/Filter (z. B. nur ein bestimmtes Team über alle Wettbewerbe hinweg verfolgen)

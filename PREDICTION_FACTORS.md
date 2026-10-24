# BetQuote – Einflussfaktoren auf das Spielergebnis

**Projektziel: keine Wett-/Quoten-App.** BetQuote ist ein statistisches Vorhersage-Tool, das auf
Basis möglichst vieler relevanter Faktoren eine **Ergebnis-Vorhersage für Fußballspiele** liefert
(idealerweise das exakte Ergebnis, z. B. "2:1") — nicht primär eine Einschätzung von
Sieg/Unentschieden/Niederlage als Wettmarkt. Diese Datei sammelt alle Faktoren, die potenziell
Einfluss auf den Ausgang eines Spiels haben und damit als Eingabe für unser Vorhersagemodell in
Frage kommen. Das ist die fachliche Grundlage, bevor wir Datenmodell/Datenbank und Algorithmus
bauen.

**Realistische Erwartungshaltung (wichtig, damit wir uns nicht selbst täuschen):** Ein exaktes
Fußball-Ergebnis ist grundsätzlich nur begrenzt vorhersagbar — nicht wegen fehlender Daten, sondern
weil die geringe Toranzahl pro Spiel (Ø ~2,5–3) dem Zufall (Abpraller, Fehlentscheidungen,
Tagesform) strukturell viel Raum lässt. Selbst die besten öffentlich bekannten Modelle (große
Buchmacher, Opta, etc.) erreichen beim exakten Ergebnis typischerweise nur ~18–22% Trefferquote,
bei 1X2 (Sieg/Remis/Sieg) ~50–55%. Unser Ziel ist daher **nicht "immer exakt richtig"**, sondern
"so nah wie mit den verfügbaren Daten und Methoden realistisch möglich" — durch die geplante
Wahrscheinlichkeitsverteilung über mögliche Ergebnisse (siehe "Ausgabeform" unten) wird zusätzlich
transparent, wie sicher sich das Modell im Einzelfall ist, statt eine falsche Präzision
vorzutäuschen.

Status je Faktor: `[ ]` gesammelt/geplant, `[x]` bereits in Datenmodell/Berechnung umgesetzt.
Priorität: 🔴 hoch (starker statistischer Zusammenhang, sollte früh rein) · 🟡 mittel · 🟢 nice-to-have.

---

## 1. Team-Form & historische Ergebnisse

- [ ] 🔴 Letzte 5–10 Spiele der Heim-Mannschaft (Ergebnis, Tore geschossen/kassiert, Heim/Auswärts getrennt betrachten)
- [ ] 🔴 Letzte 5–10 Spiele der Auswärts-Mannschaft (analog)
- [ ] 🔴 Direkter Vergleich / Head-to-Head: Ergebnisse der letzten Begegnungen beider Teams (z. B. letzte 5–10 Duelle)
- [ ] 🔴 Form-Split: wie performt Heimteam speziell *zuhause*, Auswärtsteam speziell *auswärts* (nicht nur allgemeine Form)
- [ ] 🟡 Trend innerhalb der Form (verbessernd/verschlechternd über die letzten Spiele, nicht nur Durchschnitt)
- [ ] 🟡 Ergebnisse gegen ähnlich starke Gegner (Form gegen Top-Teams vs. gegen Abstiegskandidaten getrennt)
- [ ] 🔴 **Eigenes Elo-/Stärke-Rating pro Team** (Modell-Vorschlag, nicht vom Nutzer genannt) — ein
  einzelner, laufend nach jedem Spiel aktualisierter Stärkewert je Team, der Ergebnis, Tordifferenz
  und **Stärke des jeweiligen Gegners** mitverrechnet (bekanntes Vorbild: clubelo.com). Deutlich
  aussagekräftiger als die reine Tabellenposition, weil ein 3:0 gegen den Tabellenführer anders
  gewichtet wird als ein 3:0 gegen den Absteiger — in der Fußball-Analytics-Forschung eines der
  stärksten Einzelsignale überhaupt. Keine externe Quelle nötig, lässt sich aus den bereits
  geplanten Ergebnisdaten (OpenLigaDB) selbst berechnen (Elo-Formel mit Torbonus)
- [ ] 🟡 **Expected Points (xPts)** statt nur Tabellenpunkte (Modell-Vorschlag) — wie viele Punkte
  hätte ein Team basierend auf seinem xG-Verlauf über die Saison "verdient"? Deckt auf, welche
  Teams aktuell über oder unter ihrem eigentlichen Niveau stehen (Tabellenposition allein zeigt das
  nicht) → ergänzt das xG-Regression-to-mean-Signal aus Abschnitt 2 auf Team-/Saisonebene statt nur
  Einzelspiel-Ebene

## 2. Tore / Expected Goals

- [ ] 🔴 Geschossene Tore (Ø pro Spiel, Heim/Auswärts getrennt)
- [ ] 🔴 Kassierte Tore / Gegentore (Ø pro Spiel, Heim/Auswärts getrennt) — vom Nutzer explizit genannt ("Accepted Goals")
- [ ] 🔴 Expected Goals (xG) und Expected Goals Against (xGA) — deutlich aussagekräftiger als reine Torzahlen, da sie Chancenqualität statt Zufall/Abschlussglück abbilden
- [ ] 🔴 **Expected Goals on Target (xGOT)**, auch Post-Shot-xG/PSxG genannt — vom Nutzer ergänzt.
  Verfeinerung von xG: während normales xG nur die Chance *vor* dem Schuss bewertet (Position,
  Winkel, Druck), bewertet xGOT zusätzlich den tatsächlichen Schuss selbst (Platzierung im Tor,
  Schärfe) — erfasst damit auch Abschlussqualität des Schützen und Torwart-Leistung, nicht nur
  Chancenqualität. Datenlage: **Sofascore liefert xGOT direkt pro Schuss** (`shotmap`-Array, Feld
  `xgot`) — dasselbe ohnehin schon für die Zeitsegment-Analyse (Abschnitt 10) eingeplante Modul
  deckt das mit ab, kein zusätzlicher Aufwand. FBref führt denselben Wert unter dem Namen "PSxG".
  Understat berechnet ebenfalls vergleichbare Werte, aber mit unklarerer Liga-Abdeckung
- [ ] 🟡 Über-/Unterperformance gegenüber xG (Team trifft viel besser/schlechter als die Chancenqualität erwarten lässt → Regression-to-mean-Signal)
- [ ] 🟡 Über-/Unterperformance gegenüber xGOT auf Torwart-Seite (Torwart hält deutlich mehr/weniger, als die Schussqualität erwarten lässt → Signal für Torwart-Form, unabhängig von Abwehrleistung)
- [ ] 🟢 Schüsse aufs Tor, Schüsse gesamt, Großchancen
- [ ] 🟡 **Field Tilt** (Modell-Vorschlag) — wie viel Prozent des Spiels findet im gegnerischen
  Drittel statt (gemessen z. B. über den Anteil der Pässe im letzten Drittel beider Teams), statt
  nur des reinen Ballbesitz-Prozentsatzes. Aussagekräftiger als Ballbesitz allein, da manche Teams
  bewusst mit wenig Ballbesitz spielen (Konterfußball), aber trotzdem meist im gegnerischen Drittel
  aktiv sind, wenn sie den Ball haben — Datenquelle wie Ballbesitz-Zeitsegmente über Sofascore
  (Abschnitt 10) möglich, sofern die entsprechenden Felder dort verfügbar sind (bei Umsetzung prüfen)

## 3. Tabellensituation & Kontext der Saison

- [ ] 🔴 Aktuelle Tabellenposition beider Teams
- [ ] 🟡 Punkteabstand zu Aufstiegs-/Abstiegszone bzw. Champions-League-Plätzen (Motivationsfaktor)
- [ ] 🟡 Saisonphase: früh (noch wenig aussagekräftig) vs. spät (Abstiegskampf/Meisterrennen → andere Dynamik, z. B. mehr Krampf-Fußball)
- [ ] 🟢 "Nichts mehr zu spielen"-Situationen (Team schon gerettet/abgestiegen/Meister vorzeitig fest) — Formeinbruch typisch

## 4. Kader, Aufstellung & Verletzungen

- [ ] 🔴 Spielt die Stammelf oder rotiert wird? (vom Nutzer genannt)
- [ ] 🔴 Verletzungen/Sperren von Schlüsselspielern (v. a. Torjäger, Stammtorwart, Abwehrchef)
- [ ] 🟡 Form einzelner Spieler (vom Nutzer genannt) — z. B. Torschützen der letzten Spiele, Scorer-Punkte
- [ ] 🟡 Rückkehrer aus Verletzung/Sperre (Impact meist erst nach 2-3 Spielen wieder voll da)
- [ ] 🟢 Kaderwert / Marktwert als grobe Qualitäts-Proxy (z. B. Transfermarkt-Daten)
- [ ] 🟢 Trainerwechsel (neuer Trainer → häufig kurzfristiger Formboost, "New Manager Bounce")
- [ ] 🟡 **Wettbewerbs-Rotationssignal** (Modell-Vorschlag) — ein Team, das aktuell noch in mehreren
  Wettbewerben aktiv ist (z. B. Liga + Pokal + Europapokal), rotiert erfahrungsgemäß eher die
  Stammelf als ein Team mit nur einem verbleibenden Ziel. Als Vorhersage-*Signal* gedacht (nicht
  nur nachträgliche Beobachtung): Anzahl noch laufender Wettbewerbe je Team + zeitliche Nähe zu
  einem wichtigeren Spiel (z. B. Champions-League-Rückspiel drei Tage später) erhöht die
  Rotationswahrscheinlichkeit spürbar, besonders bei Traditionsvereinen mit tiefem Kader

## 5. Spielumstände

- [ ] 🔴 Heimvorteil generell (statistisch nachweisbar, Stärke variiert je Liga/Team)
- [ ] 🟡 Anreisedistanz / Auswärtsfahrt (weite Reisen, insb. bei internationalen Wettbewerben)
- [ ] 🟡 Ruhetage seit letztem Spiel / Termindichte (Englische Wochen, Champions-League-Belastung)
- [ ] 🟡 **Europapokal-Doppelbelastung** (Modell-Vorschlag, Konkretisierung des Punkts oben) — Teams,
  die z. B. mittwochs in Champions/Europa/Conference League spielen und danach samstags in der Liga
  antreten, zeigen nachweislich schwächere Leistung (Reisestrapazen, weniger Regenerationszeit,
  höhere Rotation). Eigener Faktor statt nur allgemeiner "Ruhetage"-Betrachtung, da europäische
  Auswärtsspiele (weitere Reisen, andere Anstoßzeiten) einen stärkeren Effekt haben als normale
  Ligaspiele im Kurzabstand
- [ ] 🟡 Wichtigkeit des Spiels (Pokal vs. Liga, Derby, Abstiegsduell, "Endspiel"-Charakter)
- [ ] 🟢 **Geografische Distanz zwischen den Vereinsstandorten** (Modell-Vorschlag) — als objektive
  Kennzahl für "wie sehr ist das ein echtes Derby" statt nur gefühlter Einschätzung; geringe Distanz
  korreliert erfahrungsgemäß mit erhöhter emotionaler Intensität und Foulquote/Kartenzahl (ergänzt
  damit auch den Schiedsrichter-Abschnitt 9), unabhängig von Tabellenkonstellation
- [ ] 🟢 Wetterbedingungen (Regen, Hitze, Wind — Einfluss eher gering, aber vorhanden)
- [ ] 🟢 Rasenqualität / Stadion-Eigenheiten (kleine Plätze, Kunstrasen)
- [ ] 🟢 Zuschauerzahl / Auslastung, Stimmung (Geisterspiele hatten nachweislich Effekt auf Heimvorteil, siehe Covid-Daten)
- [ ] 🟢 **Anstoßzeit** (Modell-Vorschlag) — z. B. Montagsspiele oder frühe Sonntagsspiele gelten
  unter Fans als unbeliebt/schlechter besucht bzw. -gestimmt; nachweisbarer, aber schwacher Effekt

## 6. Buchmacher-Quoten als externer Referenzwert (nicht als Wettgrundlage)

Kein Wettmarkt-Feature im engeren Sinn, sondern reiner **Validierungs-/Vergleichswert**: Quoten der
großen Buchmacher bündeln bereits enorm viel Analyse-Know-how und eignen sich daher gut, um die
Qualität unseres eigenen Modells einzuordnen ("liegen wir grob in der Größenordnung der Profis,
oder komplett daneben?").

- [ ] 🟢 Buchmacher-Quoten historischer Spiele als Referenz-/Benchmark-Datensatz fürs Backtesting (nicht als Modell-Input, sondern als Maßstab: wie gut ist unser Modell im Vergleich zum Markt?)
- [ ] 🟢 Abweichung unserer Vorhersage vom Marktkonsens als Qualitäts-/Auffälligkeits-Signal (große Abweichung → entweder haben wir einen Faktor entdeckt, den der Markt unterschätzt, oder unser Modell hat einen blinden Fleck — beides interessant zu wissen)

## 7. Taktik & Matchup

- [ ] 🟡 Spielsystem/Taktik beider Teams (z. B. hohes Pressing vs. Kontertaktik — manche Matchups begünstigen bestimmte Spielweisen)
- [ ] 🟢 Formations-Notation (4-4-2, 4-2-3-1, 3-5-2 etc.) als grober Zusatz-Baustein — vom Nutzer
  angefragt. Recherche-Ergebnis: **die Formation allein ist laut Sportwissenschaft ein schwacher
  Vorhersage-Faktor** — mehrere Studien finden keinen signifikanten Effekt der reinen
  Formationsbezeichnung auf den Spielausgang, da dieselbe Formation offensiv oder defensiv sehr
  unterschiedlich ausgelegt werden kann (ein 4-2-3-1 ist nicht automatisch aggressiv). Ein
  Formationswechsel *während* des Spiels korreliert in Bundesliga-Daten sogar eher mit sinkender
  Gewinnwahrscheinlichkeit — vermutlich Folge, nicht Ursache (Wechsel oft erst bei Rückstand).
  Entscheidend ist laut Forschung nicht die Formation selbst, sondern das **Matchup zwischen den
  Spielstilen** (siehe folgender Punkt) — deshalb hier niedrig priorisiert und nur als Kontext-Info
  zum Spielsystem-Punkt oben gedacht, nicht als eigenständig aussagekräftiges Feature
- [ ] 🟢 Historische Schwäche gegen bestimmten Spielstil (Team X tut sich gegen tief stehende Gegner schwer)
- [ ] 🟢 Standardsituationen-Stärke (Ecken/Freistöße als Torquelle, insb. bei ausgeglichenen Spielen relevant)
- [ ] 🟡 **Progressive Passes** (erfolgreiche Pässe, die den Ball deutlich — FBref-Definition: mind.
  ~10 Meter näher Richtung gegnerisches Tor — voranbringen, oder jeder erfolgreiche Pass in den
  gegnerischen Strafraum) — vom Nutzer angestoßene Idee ("wie viele Gegenspieler hebelt ein Pass
  aus"). Die exakte Metrik dafür heißt **Packing** (zählt pro erfolgreichem Pass/Dribbling die
  Anzahl der dadurch "hinter den Ball" gebrachten Gegenspieler, entwickelt von Reinartz/Hegeler,
  Firma IMPECT) — **ist jedoch proprietär und wird von IMPECT als B2B-Produkt verkauft** (inzwischen
  auch mit SAP als Partner), kein öffentlicher/kostenloser Zugang gefunden. **Progressive Passes**
  (FBref, Spalte "PrgP", pro Spieler und Team verfügbar) ist die offen zugängliche, verwandte
  Ersatzmetrik — misst kein exaktes "Anzahl ausgehebelter Gegner", aber dasselbe Grundprinzip
  (Linienüberwindung/Spielverlagerung durch Pässe), correliert stark mit Packing. Da FBref ohnehin
  als Quelle eingeplant ist (siehe Datenquellen-Tabelle unten), kein zusätzlicher Beschaffungsaufwand

## 8. Wettbewerbsspezifika

- [ ] 🟡 Liga-spezifische Baseline (Toranzahl/Heimvorteil variiert stark zwischen Ligen, z. B. Bundesliga torreicher als Serie A)

## 9. Schiedsrichter

Vom Nutzer ergänzt: Schiedsrichter haben einen messbaren eigenen statistischen Fußabdruck (es gibt
einen Liga-Durchschnitt an Karten/Elfmetern pro Spiel, und jeder Schiri weicht davon nach oben oder
unten ab) — das wirkt sich nicht nur auf Karten-Zusatzmärkte aus, sondern auf das Spielergebnis
selbst: ein Platzverweis kann durch die entstehende Unterzahl das Ergebnis kippen, eine hohe
Elfmeterquote begünstigt tendenziell torreichere Spiele.

- [ ] 🟡 Karten-Schnitt des Schiedsrichters (Gelbe/Rote Karten pro Spiel im Vergleich zum Liga-Durchschnitt — "strenger" vs. "lockerer" Schiri)
- [ ] 🟡 Elfmeterquote des Schiedsrichters (Elfmeter pro Spiel im Vergleich zum Liga-Durchschnitt)
- [ ] 🟢 Wahrscheinlichkeit einer Rote-Karte-/Unterzahl-Situation als Ergebnis-Einflussfaktor (indirekt: erhöht bei "strengem" Schiri die Varianz des Ergebnisses)
- [ ] 🟢 Team-spezifische Historie mit einzelnen Schiedsrichtern (manche Team/Schiri-Paarungen fallen historisch auffällig aus — kleine Stichprobe, daher nur als schwaches Signal)
- [ ] 🟢 **Schiedsrichter-Heimmannschaftsbias** (Modell-Vorschlag) — in internationalen Studien
  teils nachgewiesener leichter Trend, dass Schiedsrichter tendenziell knapp zugunsten der
  Heimmannschaft entscheiden (z. B. bei Nachspielzeit-Länge oder Elfmeter-Grenzfällen). Für die
  deutschen Ligen selbst nicht belastbar isoliert erforscht gefunden, Effekt in der Literatur
  ohnehin klein — niedrigste Priorität, eher der Vollständigkeit halber aufgenommen

## 10. Zeitliche Verteilung innerhalb des Spiels (15-Minuten-Segmente)

Vom Nutzer ergänzt: nicht nur *ob* ein Team gut spielt, sondern *wann* im Spiel — manche Teams
starten stark und lassen nach, andere kommen erst nach der Halbzeit auf Touren. Betrachtungszeitraum
jeweils zweifach: Ø der letzten 5–10 Spiele des Teams UND Ø der letzten 5 direkten
Aufeinandertreffen (Head-to-Head) — beides getrennt nach Heim/Auswärts, da sich das
Heim-/Auswärts-Zeitverhalten unterscheiden kann.

- [ ] 🟡 Tor-Verteilung pro 15-Minuten-Segment (0-15/15-30/30-45/45-60/60-75/75-90) — aus Tor-Minuten
  ableitbar, die OpenLigaDB bereits mitliefert; keine Zusatzquelle nötig. Zeigt z. B. "Team X
  erzielt 35 % seiner Saisontore in den letzten 15 Minuten" als Proxy für Schlussoffensive/starke
  Einwechselspieler bzw. "Team Y kassiert überdurchschnittlich viele Gegentore direkt nach der
  Halbzeitpause" als mögliches Konzentrationsproblem
- [ ] 🟡 Echte Druck-/Intensitätsmessung pro Zeitsegment (Ballbesitz, Schüsse, gefährliche Angriffe
  je Segment) — **Datenlage-Einschränkung**: OpenLigaDB liefert das nicht, die offizielle
  Bundesliga-Quelle (Opta/Stats Perform, auch von Kicker/FotMob genutzt) ist ein Enterprise-Produkt
  ohne öffentliches Preismodell (vermutlich vierstellig/Monat), praktisch nicht zugänglich für
  dieses Projekt. Geplante Lösung: **inoffizielle Sofascore-API** (`api.sofascore.com`, liefert
  Ballbesitz/Schüsse/xG pro Halbzeit, kein API-Key nötig) als optionales, sauber gekapseltes Modul
  einbinden — bewusst *nicht* als harte Abhängigkeit fürs Kernmodell, da: (a) undokumentiert und
  jederzeit ohne Vorwarnung änderbar, (b) Sofascores eigene Nutzungsbedingungen kommerzielle Nutzung
  untersagen. **Bestätigt vom Nutzer: BetQuote ist ein rein privates Projekt, nur für den Eigenbedarf,
  keine kommerzielle Nutzung/kein öffentliches Angebot** — das entschärft den ToS-Punkt (b)
  deutlich, Punkt (a) bleibt aber bestehen (technische Instabilität unabhängig vom Nutzungszweck).
  Modell muss trotzdem auch ohne dieses Modul funktionieren, es reichert nur an, wenn verfügbar
- [ ] 🟢 Vergleich Erste-Halbzeit- vs. Zweite-Halbzeit-Stärke als grobe Einordnung (feiner als nur
  90-Minuten-Endergebnis, aber ohne die Feinheit echter 15-Minuten-Segmente, allein aus
  Halbzeit-/Endergebnis-Differenz berechenbar — ebenfalls ohne Zusatzquelle möglich)

---

## Entscheidungen (Stand: geklärt mit Nutzer)

### Ziel-Wettbewerbe
**Aktualisiert (Nutzer-Entscheidung, ersetzt die ursprüngliche Wahl):** 1. Bundesliga, 2. Bundesliga,
**La Liga (Spanien)** — 3. Liga und DFB-Pokal wurden bewusst wieder rausgenommen, um den Scope zu
verschlanken. La Liga kommt neu dazu. Betrifft auch die Datenquellen (siehe TECH_STACK.md): für
La Liga wird bewusst **football-data.org statt OpenLigaDB** verwendet (OpenLigaDB deckt La Liga nur
über ein unzuverlässiges, community-gepflegtes Schema mit wechselnden Liga-Shortcuts ab — nicht die
robuste Kernliga der Plattform wie bl1/bl2). football-data.org deckt die 12 großen europäischen
Top-Wettbewerbe im kostenlosen Tier ab, inkl. La Liga, mit sauberem/stabilem Schema — einziger
Nachteil: Live-Scores dort leicht verzögert statt nahezu live wie bei OpenLigaDB, was für ein
Vorhersage-Tool (das ohnehin vor Anpfiff tippt, nicht auf Live-Wetten ausgelegt ist) unkritisch ist.

**Präzisierung zum DFB-Pokal (Nutzer-Entscheidung, kleine Kurskorrektur):** Der Pokal bleibt weiterhin
**kein eigener Vorhersage-Wettbewerb** (kein eigener Tab, kein eigener Wochen-Feed, keine eigene
Tabelle/Vorhersage) — daran ändert sich nichts. Der **OpenLigaDB-Import für den Pokal wird aber
wieder aktiviert**, ausschließlich um den **aktuellen DFB-Pokalsieger** zu ermitteln, der als kleines
Auszeichnungs-Icon neben dem entsprechenden Team in der Liga-Tabelle angezeigt wird (siehe
APP_STRUCTURE.md Abschnitt 1b, "Auszeichnungs-Icons"). Der Pokal-Import läuft also technisch wieder,
aber nur als Datenquelle für dieses eine Detail, nicht als reaktivierter Vorhersage-Wettbewerb.

### Vorhersageziel
Primäres Ziel ist das **exakte Ergebnis** (z. B. "2:1") als konkrete Tipp-Empfehlung — kein
Wettmarkt-Tool. 1X2, Over/Under 2.5 und BTTS fallen als Nebenprodukt der gleichen zugrunde
liegenden Toreverteilung je Team automatisch mit ab (siehe Modellansatz) und dienen vor allem als
zusätzliche Plausibilitätsebene ("passt der exakte Tipp auch zur wahrscheinlichsten Tendenz?"),
nicht als eigenständige Wettmärkte.

### Ausgabeform
Das Modell zeigt **nicht nur eine einzelne Tipp-Zahl**, sondern eine Wahrscheinlichkeitsverteilung
über die plausibelsten Ergebnisse, mit dem wahrscheinlichsten Ergebnis klar als Haupttipp
hervorgehoben, z. B.:

```
Bayern München vs. Borussia Dortmund
Tipp: 2:1  (13 %)

Weitere wahrscheinliche Ergebnisse:
1:1  – 11 %
2:0  – 9 %
1:0  – 9 %
3:1  – 7 %
...
```

Das macht transparent, wie sicher sich das Modell ist, statt eine falsche Präzision vorzutäuschen
(siehe "Realistische Erwartungshaltung" oben) — ein Tipp mit 13% Wahrscheinlichkeit ist immer noch
der beste verfügbare Tipp, auch wenn er öfter falsch als richtig liegen wird.

### Farbliche Abstufung der Exakt-Ergebnis-Prozentzahl

Vom Nutzer ergänzt, nachdem er nachgefragt hatte, was die Prozentzahl in Klammern hinter dem Tipp
(z. B. "4:0 (13 %)") bedeutet: **das ist die Wahrscheinlichkeit für genau dieses eine exakte
Ergebnis** unter allen möglichen Ergebnissen — nicht zu verwechseln mit der danebenstehenden
1X2-Tendenz (Heim/Remis/Auswärts, die sich zu 100 % addiert). Beide Zahlen können weit auseinander
liegen (z. B. Heimsieg-Tendenz 95 %, aber das wahrscheinlichste Einzelergebnis nur 13 %, weil sich
viele plausible Heimsiege wie 2:0, 3:0, 3:1, 4:0 die Wahrscheinlichkeit teilen).

**Fünf Farbstufen** für diese Prozentzahl (je höher, desto sicherer ist sich das Modell bei genau
diesem exakten Ergebnis):

| Bereich | Bedeutung | Farbe (Richtung) |
|---|---|---|
| < 8 % | sehr unsicher/ausgeglichen, Wahrscheinlichkeit verteilt sich auf viele ähnlich plausible Ergebnisse | neutral/grau |
| 8–11 % | unterdurchschnittlich, aber noch der beste verfügbare Tipp | kühles Blau/Grau-Blau |
| 11–15 % | **typischer guter Wert** — Bereich, in dem der wahrscheinlichste Einzeltipp normalerweise liegt | neutrales bis leicht positives Blau/Grün |
| 15–20 % | überdurchschnittlich sicher | Grün |
| > 20 % | ungewöhnlich hoch, Modell ist sich für ein exaktes Ergebnis selten so sicher | kräftiges/gesättigtes Grün |

**Wichtige Abgrenzung zur Konfidenz-Markierung** (siehe nächster Abschnitt): Das ist eine
**kontinuierliche Farbskala auf der Prozentzahl selbst** (jede Spiel-Karte bekommt je nach Wert
einen anderen Farbton auf dieser einen Zahl), während die Konfidenz-Markierung ein **binäres
Karten-weites Signal** ist (Karte wird komplett hervorgehoben oder nicht) und auf der
**1X2-Tendenz** basiert, nicht auf der Exakt-Ergebnis-Wahrscheinlichkeit — beide Signale bleiben
nebeneinander bestehen, sie ersetzen sich nicht gegenseitig. Genaue Farbwerte (Hex-Codes je Stufe,
WCAG-Kontrast in beiden Themes) sind vom `designer`-Agenten bei der Umsetzung festzulegen, gemäß
den globalen Visual-Design-Defaults (Light/Dark-Theme, Kontrast-Anforderungen).

### Konfidenz-Markierung ("worauf würdest du am ehesten tippen?")

Vom Nutzer ergänzt, **bewusst ohne jeden Wettbezug**: eine visuelle Hervorhebung für Spiele, bei
denen unser **eigenes Modell** eine besonders hohe Konfidenz hat — abgeleitet rein aus der Form der
eigenen Wahrscheinlichkeitsverteilung (z. B. Top-Ergebnis liegt deutlich über einem Schwellenwert,
oder großer Abstand zum zweitwahrscheinlichsten Ergebnis/zur zweitwahrscheinlichsten 1X2-Tendenz).
**Kein Abgleich mit Buchmacher-Quoten** (das wäre Value-Betting-Logik und würde echte Live-Odds
mehrerer Buchmacher voraussetzen — explizit nicht gewollt, siehe unten). Reines Signal aus der
eigenen Modell-Sicherheit heraus: "dieses Spiel schätzt unser Modell ungewöhnlich eindeutig ein",
nicht "hier lohnt sich eine Wette". Konkrete Schwellenwerte (ab wann gilt eine Verteilung als
"eindeutig genug" für die Markierung) sind eine Kalibrierungsfrage für die Umsetzung, keine
grundsätzliche Vorgabe hier.

**Explizit abgegrenzt und NICHT Teil des Projekts** (Nutzer-Rückfrage zu professionellen
Sportwett-Strategien wurde diskutiert und bewusst verworfen, um beim ursprünglichen Charakter als
reines Vorhersage-Tool zu bleiben — siehe Projektziel-Absatz ganz oben):
- **Value-Betting** (eigene Wahrscheinlichkeit vs. Buchmacher-Quote, wetten nur bei positiver
  Erwartungswert-Differenz) — würde eine aktive Quoten-Vergleichslogik und die Interpretation als
  Handlungsempfehlung erfordern
- **Bankroll-Management** (Kapitaleinsatz-Rechner, Wettkapital-Verwaltung) — setzt voraus, dass
  überhaupt reale Wetten getätigt werden, was hier nicht der Fall ist
- **Arbitrage-Betting** (Quotenunterschiede zwischen mehreren Buchmachern ausnutzen) — bräuchte
  Live-Odds-Anbindung an mehrere Buchmacher gleichzeitig, komplett anderer Projekt-Charakter
- Buchmacher-Quoten bleiben wie in Abschnitt 6 beschrieben ausschließlich ein **Referenzwert fürs
  Backtesting** ("liegen wir grob in der Größenordnung der Profis?"), tauchen aber nicht als
  aktiver Vergleichswert in der normalen Spiel-Ansicht auf

### Modellansatz
Lernendes Modell von Anfang an (nicht nur regelbasiert). Geplanter Aufbau in Stufen:
1. **Poisson-/Dixon-Coles-Modell** als statistisches Grundgerüst für Torerwartung je Team
   (liefert 1X2, O/U, BTTS und exaktes Ergebnis gleichzeitig aus einer Toreverteilung)
2. **Gradient-Boosting-Modell** (z. B. LightGBM/XGBoost) auf den 🔴/🟡-Faktoren aus dieser Datei
   als Feature-Set, um über die reine Tor-Poisson-Baseline hinaus Muster zu erfassen (Form,
   Verletzungen, Tabellensituation etc.)
3. Backtesting beider Ansätze gegeneinander und ggf. Ensemble/Kombination

### Datenquellen

**Aktiv eingeplant:**

| Quelle | Zweck | Abdeckung | Historie | Kosten |
|---|---|---|---|---|
| **OpenLigaDB** (`api.openligadb.de`) | Primärquelle, laufender Betrieb | bl1, bl2, bl3, dfb — genau unsere 4 Wettbewerbe | bl1 ab ~2001, bl2 ab ~2006, dfb ab ~2007, bl3 ab ~1991 (lückenhaft), laufend aktuell gepflegt | Kostenlos, keine Auth, kein Rate-Limit bekannt |
| **Kaggle "AllBundesligaGamesEver"** | Einmaliger historischer Import | nur 1. Bundesliga | ab 1963 (Gründungssaison) | Kostenlos, statischer Download |
| **Understat** (Scraping) | Optionale xG-Anreicherung | nur 1. Bundesliga | ab Saison 2014/15 | Kostenlos, aber kein offizielles API (Scraping HTML-embedded JSON) — als optionales, austauschbares Modul bauen, nicht als harte Abhängigkeit |
| **Sofascore** (inoffizielle API, `api.sofascore.com`) | Optionale Ballbesitz-/Schuss-Statistik pro Halbzeit (Zeitsegment-Analyse, siehe Abschnitt 10) **plus xGOT pro Schuss** (`shotmap`-Feld `xgot`, siehe Abschnitt 2 — eigentlich ein Opta-Wert, hier kostenlos mit erreichbar) **plus echte laufende Spielminute inkl. Nachspielzeit** (vom Nutzer gewünscht — OpenLigaDB liefert dafür kein Feld, nur Tor-Zeitpunkte, siehe APP_STRUCTURE.md Abschnitt 1 "Live-Spielzeit") | vermutlich alle 4 Wettbewerbe, ungeprüft im Detail | laufend aktuell | Kostenlos, undokumentiert/inoffiziell — ok für privates, nicht-kommerzielles Projekt (siehe Abschnitt 10), aber jederzeit ohne Vorwarnung änderbar |
| **FBref** (Scraping, `fbref.com`) | Ergänzende aggregierte Team-/Spieler-Statistiken (xG, **PSxG = xGOT**, "per 90"-Kennzahlen), v. a. für 2./3. Liga, wo Understat nichts liefert | breite Liga-Abdeckung, Details je Liga zu prüfen bei Umsetzung | gute Historie, seit 2026 auch historische Daten ohne Premium-Sperre frei zugänglich | Kostenlos, kein offizielles API — Scraping mit striktem Rate-Limit (max. 1 Request/3 Sek., sonst IP-Sperre bis zu 24h) |

**Wichtige Einschränkung, die wir bewusst akzeptieren** (keine künstliche Vereinheitlichung):
"Seit Gründung" ist nur für die 1. Liga (1963) überhaupt an Daten verfügbar. Die 2. Liga existiert
erst seit 1974 — digitale, verlässliche Daten dafür beginnen praktisch erst in den 1990er/2000er
Jahren. La Liga hat ihre eigene, unabhängige Historie über football-data.org (siehe oben). Jeder
Wettbewerb startet die Datenhistorie beim jeweils frühesten verlässlich verfügbaren Punkt, kein
gemeinsames künstliches Startjahr über alle drei hinweg.

*(Hinweis: 3. Liga und DFB-Pokal waren ursprünglich ebenfalls im Scope und sind an mehreren Stellen
dieser Datei noch als historischer Recherche-Kontext erwähnt — z. B. bei der FBref/StatsBomb-
Einordnung weiter unten. Das betrifft nicht mehr den aktuellen Projekt-Scope, siehe "Entscheidungen
→ Ziel-Wettbewerbe" weiter unten für den aktuellen Stand.)*

xG-Daten (Understat, FBref) werden als optionales Zusatzmodul eingebaut, primär für die 1. Liga
(Understat ab 2014/15) bzw. dort wo FBref entsprechende Werte führt. Wo keine xG-Quelle greift,
läuft das Modell ohne xG und nutzt stattdessen die anderen 🔴/🟡-Faktoren aus dieser Datei
(Torschüsse, Form etc.).

Da mehrere Quellen kombiniert werden, braucht es ein **Team-/Spiel-Mapping zwischen den Quellen**
(unterschiedliche Schreibweisen wie "Bayern München" vs. "FC Bayern München" vs. "Bayern") — eigener
technischer Baustein beim Datenimport, z. B. über eine eigene Team-ID mit bekannten Aliasnamen pro
Quelle, plus Datum+Team-Paarung als Fallback-Schlüssel zum Matchen von Spielen über Quellen hinweg.

**Geprüft, aktuell nicht eingeplant** (falls später doch Bedarf entsteht):

| Quelle | Warum aktuell nicht eingeplant |
|---|---|
| **StatsBomb Open Data** | Fachlich beste Datenqualität (Event-Daten mit Koordinaten, StatsBomb-360-Daten), aber Bundesliga nur lückenhaft vertreten (nur Saisons 2017/18–2019/20 plus eine Leverkusen-Saison 2023/24), keine 2./3. Liga/Pokal. Mögliche Zukunftsoption für sehr detaillierte taktische Analyse einzelner Spiele, falls gewünscht — aber kein Ersatz für eine laufende Hauptquelle |
| **API-Football** | Free-Tier nur 100 Requests/Tag — bei 4 Wettbewerben mit regelmäßigem Import zu knapp bemessen; Endstatistiken nur als Gesamtwerte (keine Zeitsegmente) |
| **football-data.org** | Free-Tier deckt nur 12 Top-Wettbewerbe ab, davon nur die 1. Bundesliga — kein Mehrwert ggü. OpenLigaDB für unsere 4 Zielwettbewerbe |
| **AWS Bundesliga Match Facts / Opta (Stats Perform)** | Die professionelle Quelle hinter Kicker, FotMob, WhoScored und den DFL-Broadcast-Grafiken (Win Probability, Attacking Zones, Most Pressed Player) — fachlich die beste verfügbare Tiefe, aber ein B2B-Enterprise-Produkt ohne öffentliches Preismodell/keine Möglichkeit zum Selbst-Abonnieren als Privatperson |

---

## Nächste Schritte

- [ ] Prioritätenliste (🔴-Faktoren zuerst) in ein konkretes Datenmodell überführen (Entities: Team,
  Spiel, Spieler, Verletzung, Liga, Saison, Wettbewerb, ...)
- [ ] OpenLigaDB-Anbindung als Datenimport-Job bauen (alle 4 Wettbewerbe, alle verfügbaren Saisons)
- [ ] Kaggle-Datensatz einmalig importieren für 1.-Liga-Historie ab 1963, auf unser Datenmodell mappen
- [ ] Understat-Scraping-Modul als optionale xG-Anreicherung für 1. Liga ab 2014/15 (sauber
  gekapselt, damit es bei ToS-/Verfügbarkeitsproblemen ohne Kernauswirkung deaktivierbar ist)
- [ ] Poisson-/Dixon-Coles-Baseline-Modell für Torerwartung implementieren (liefert 1X2, O/U 2.5,
  BTTS, exaktes Ergebnis aus einer gemeinsamen Toreverteilung)
- [ ] Gradient-Boosting-Modell (LightGBM/XGBoost) auf den restlichen Faktoren als zweite Stufe
- [ ] Backtesting-Strategie: eigene Vorhersagen gegen echte historische Ergebnisse prüfen (Trefferquote
  exaktes Ergebnis, 1X2, O/U, BTTS getrennt auswerten); optional zusätzlich gegen Buchmacher-Quoten
  als Referenzmaßstab einordnen (siehe Abschnitt 6) — rein zur Qualitätseinordnung, kein Wettbezug

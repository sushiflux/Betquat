/**
 * Fuenf-Stufen-Farbskala fuer die Exakt-Ergebnis-Wahrscheinlichkeit auf der Spiel-Karte (siehe
 * PREDICTION_FACTORS.md "Farbliche Abstufung der Exakt-Ergebnis-Prozentzahl") - das ist die
 * Wahrscheinlichkeit fuer GENAU das getippte Ergebnis (z.B. "4:0 (13 %)"), nicht zu verwechseln
 * mit der 1X2-Tendenz-Konfidenz (eigenes, binaeres Signal auf der ganzen Karte, siehe
 * SpielKarte.tsx "hoheKonfidenz" - beide Signale bleiben bewusst getrennt).
 *
 * Grenzen laut Vorgabe: <8% neutral, 8-11% kuehles Blau, 11-15% neutrales/leicht positives
 * Blau-Gruen, 15-20% Gruen, >20% kraeftiges Gruen. Farbwerte selbst liegen als CSS-Variablen in
 * index.css (--c-exaktquote-1..5, je Theme), hier wird nur die Tailwind-Klasse dafuer gemappt.
 */
export function exaktquoteFarbklasse(wahrscheinlichkeit: number): string {
  if (wahrscheinlichkeit < 0.08) return 'text-exaktquote-1'
  if (wahrscheinlichkeit < 0.11) return 'text-exaktquote-2'
  if (wahrscheinlichkeit < 0.15) return 'text-exaktquote-3'
  if (wahrscheinlichkeit < 0.2) return 'text-exaktquote-4'
  return 'text-exaktquote-5'
}

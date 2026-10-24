/**
 * Single source of truth fuer den Projektnamen (siehe CLAUDE.md: "Project/product name as a
 * single source of truth"). Jede Stelle im Frontend, die den Namen braucht (Seitentitel, Header,
 * Footer, ...), importiert von hier statt den String zu wiederholen. Eine spaetere Umbenennung
 * soll nur diese Datei betreffen.
 */
export const branding = {
  name: 'BetQuote',
  tagline: 'Fußball-Ergebnis-Vorhersage für 1./2. Bundesliga & La Liga',
} as const

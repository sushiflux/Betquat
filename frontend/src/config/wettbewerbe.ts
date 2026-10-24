/**
 * Land -> Liga(n) -> WettbewerbId, fuer die Tab-Navigation (APP_STRUCTURE.md "Grundprinzip").
 *
 * ANNAHME/Entscheidung (dokumentiert statt geraten, siehe Task-Vorgabe): Die Backend-DTOs
 * (LigaTabelleDto, StatistikWettbewerbDto, SpielListeDto) liefern aktuell kein "Land"-Feld fuer
 * einen Wettbewerb, obwohl die Wettbewerb-Entity selbst bereits ein Land-Property hat (siehe
 * backend/Data/Entities/Wettbewerb.cs). Die Land/Liga-Tab-Struktur wird deshalb hier als
 * statische Frontend-Konfiguration ueber die bekannten WettbewerbIds nachgebildet, nicht dynamisch
 * aus der API abgeleitet. WettbewerbIds gegen die laufende Dev-API verifiziert (GET
 * /api/tabelle/{id}): 1 = "1. Bundesliga", 2 = "2. Bundesliga", 5 = "La Liga". Ids 3 ("3. Liga")
 * und 4 ("DFB-Pokal") existieren in der DB noch aus einer frueheren Scope-Fassung (siehe
 * PREDICTION_FACTORS.md "Entscheidungen -> Ziel-Wettbewerbe": beide wurden bewusst wieder aus dem
 * Projekt-Scope genommen) und werden hier deshalb nicht eingebunden.
 *
 * Falls das Backend spaeter ein echtes "land"-Feld in den DTOs ergaenzt, kann diese Datei durch
 * eine dynamische Ableitung ersetzt werden - bis dahin ist das die einzige Stelle, die angepasst
 * werden muss, um einen Wettbewerb zu verschieben/hinzuzufuegen (vgl. CLAUDE.md "Project/product
 * name as a single source of truth" - gleiches Prinzip hier auf die Liga-Struktur angewendet).
 */

export interface LigaConfig {
  /** Stabiler Key fuer URL-Query-Param & React-State, unabhaengig vom Anzeigenamen. */
  key: string
  label: string
  wettbewerbId: number
}

export interface LandConfig {
  key: string
  label: string
  flag: string
  ligen: LigaConfig[]
}

export const LAENDER: LandConfig[] = [
  {
    key: 'de',
    label: 'Deutschland',
    flag: '🇩🇪',
    ligen: [
      { key: 'bl1', label: '1. Bundesliga', wettbewerbId: 1 },
      { key: 'bl2', label: '2. Bundesliga', wettbewerbId: 2 },
    ],
  },
  {
    key: 'es',
    label: 'Spanien',
    flag: '🇪🇸',
    ligen: [{ key: 'laliga', label: 'La Liga', wettbewerbId: 5 }],
  },
]

export function findLiga(landKey: string, ligaKey: string): { land: LandConfig; liga: LigaConfig } | null {
  const land = LAENDER.find((l) => l.key === landKey)
  if (!land) return null
  const liga = land.ligen.find((l) => l.key === ligaKey)
  if (!liga) return null
  return { land, liga }
}

export const DEFAULT_LAND = LAENDER[0]
export const DEFAULT_LIGA = DEFAULT_LAND.ligen[0]

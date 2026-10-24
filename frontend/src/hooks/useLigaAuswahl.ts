import { useCallback, useEffect, useState } from 'react'
import { DEFAULT_LAND, DEFAULT_LIGA, findLiga, LAENDER } from '../config/wettbewerbe'

/**
 * Land/Liga-Auswahl als React-State, gespiegelt in die URL-Query-Params ?land=&liga= (fuers
 * Bookmarking, siehe Task-Vorgabe "waere sinnvoll, aber kein Muss"). Liest den Startwert einmalig
 * aus der URL, schreibt Aenderungen per replaceState zurueck (kein zusaetzlicher
 * History-Eintrag pro Tab-Klick - ein Tab-Wechsel ist keine "Seite", die per Zurueck-Button
 * einzeln durchklickbar sein soll).
 */
export function useLigaAuswahl() {
  const [landKey, setLandKeyState] = useState<string>(() => {
    const params = new URLSearchParams(window.location.search)
    const land = params.get('land')
    return LAENDER.some((l) => l.key === land) ? land! : DEFAULT_LAND.key
  })

  const [ligaKey, setLigaKeyState] = useState<string>(() => {
    const params = new URLSearchParams(window.location.search)
    const land = params.get('land')
    const liga = params.get('liga')
    if (land && liga && findLiga(land, liga)) return liga
    return DEFAULT_LIGA.key
  })

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    params.set('land', landKey)
    params.set('liga', ligaKey)
    const newUrl = `${window.location.pathname}?${params.toString()}`
    window.history.replaceState(null, '', newUrl)
  }, [landKey, ligaKey])

  const selectLand = useCallback((key: string) => {
    const land = LAENDER.find((l) => l.key === key)
    if (!land) return
    setLandKeyState(key)
    // Beim Land-Wechsel automatisch die erste Liga dieses Landes waehlen, statt einen
    // ungueltigen Liga-Key aus dem vorherigen Land stehen zu lassen.
    setLigaKeyState(land.ligen[0].key)
  }, [])

  const selectLiga = useCallback((key: string) => {
    setLigaKeyState(key)
  }, [])

  const resolved = findLiga(landKey, ligaKey) ?? { land: DEFAULT_LAND, liga: DEFAULT_LIGA }

  return {
    land: resolved.land,
    liga: resolved.liga,
    selectLand,
    selectLiga,
  }
}

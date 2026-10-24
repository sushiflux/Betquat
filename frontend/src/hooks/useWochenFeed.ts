import { useEffect, useState } from 'react'
import { ApiError, api } from '../api/client'
import type { WochenFeedDto } from '../types/api'
import type { KwRef } from '../utils/kalenderwoche'

/**
 * Wochen-Feed mit Polling (analog useTabelle, gleiches 30s-Intervall) - damit ein Spiel, das
 * gerade von "live" auf "beendet" wechselt, im Feed auch tatsaechlich sichtbar wird und die
 * dynamische Sortierung (siehe SpielKarte-Sortierung in WochenFeed.tsx) einen Grund hat, neu zu
 * greifen. Ohne Polling wuerde ein bereits geladener Wochen-Feed nie von sich aus den Status
 * eines laufenden Spiels aktualisieren.
 */
export function useWochenFeed(kw: KwRef, wettbewerbIds: number[], pollMs = 30_000) {
  const [daten, setDaten] = useState<WochenFeedDto | null>(null)
  const [ladend, setLadend] = useState(true)
  const [fehler, setFehler] = useState<string | null>(null)

  useEffect(() => {
    let abgebrochen = false
    setLadend(true)
    setFehler(null)
    setDaten(null)

    async function laden() {
      try {
        const res = await api.getWoche(kw.jahr, kw.kw, wettbewerbIds)
        if (!abgebrochen) setDaten(res)
      } catch (err) {
        if (!abgebrochen) setFehler(err instanceof ApiError ? err.message : 'Wochen-Feed konnte nicht geladen werden.')
      } finally {
        if (!abgebrochen) setLadend(false)
      }
    }

    void laden()
    const interval = window.setInterval(() => void laden(), pollMs)

    return () => {
      abgebrochen = true
      window.clearInterval(interval)
    }
    // wettbewerbIds als Array-Referenz ist innerhalb eines Liga-Tabs stabil (kommt aus der
    // statischen Config), daher hier unproblematisch als Dependency ueber JSON-Vergleich.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [kw.jahr, kw.kw, JSON.stringify(wettbewerbIds), pollMs])

  return { daten, ladend, fehler }
}

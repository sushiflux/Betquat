import { useEffect, useState } from 'react'
import { ApiError, api } from '../api/client'
import type { LigaTabelleDto } from '../types/api'

/** Live-Tabelle mit Polling, damit Platzierungswechsel waehrend eines laufenden Spiels sichtbar animieren (APP_STRUCTURE.md 1b) - das Backend selbst hat (noch) keinen Push/WebSocket-Kanal, daher Intervall-Polling als pragmatische Grundlage. */
export function useTabelle(wettbewerbId: number, pollMs = 30_000) {
  const [daten, setDaten] = useState<LigaTabelleDto | null>(null)
  const [ladend, setLadend] = useState(true)
  const [fehler, setFehler] = useState<string | null>(null)

  useEffect(() => {
    let abgebrochen = false
    setLadend(true)
    setFehler(null)
    setDaten(null)

    async function laden() {
      try {
        const res = await api.getTabelle(wettbewerbId)
        if (!abgebrochen) setDaten(res)
      } catch (err) {
        if (!abgebrochen) setFehler(err instanceof ApiError ? err.message : 'Tabelle konnte nicht geladen werden.')
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
  }, [wettbewerbId, pollMs])

  return { daten, ladend, fehler }
}

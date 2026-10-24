import { useEffect, useState } from 'react'
import { ApiError, api } from '../api/client'
import type { SpielDetailDto } from '../types/api'

export function useSpielDetail(spielId: number | null) {
  const [daten, setDaten] = useState<SpielDetailDto | null>(null)
  const [ladend, setLadend] = useState(false)
  const [fehler, setFehler] = useState<string | null>(null)

  useEffect(() => {
    if (spielId === null) {
      setDaten(null)
      setFehler(null)
      return
    }
    let abgebrochen = false
    setLadend(true)
    setFehler(null)
    api
      .getSpielDetail(spielId)
      .then((res) => {
        if (!abgebrochen) setDaten(res)
      })
      .catch((err: unknown) => {
        if (!abgebrochen) setFehler(err instanceof ApiError ? err.message : 'Spiel-Detail konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!abgebrochen) setLadend(false)
      })
    return () => {
      abgebrochen = true
    }
  }, [spielId])

  return { daten, ladend, fehler }
}

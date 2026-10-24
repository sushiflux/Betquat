import { useEffect, useState } from 'react'
import { ApiError, api } from '../api/client'
import type { StatistikDrilldownDto, StatistikWettbewerbDto } from '../types/api'

export function useStatistikUebersicht() {
  const [daten, setDaten] = useState<StatistikWettbewerbDto[] | null>(null)
  const [ladend, setLadend] = useState(true)
  const [fehler, setFehler] = useState<string | null>(null)

  useEffect(() => {
    let abgebrochen = false
    api
      .getTrefferquoteUebersicht()
      .then((res) => {
        if (!abgebrochen) setDaten(res)
      })
      .catch((err: unknown) => {
        if (!abgebrochen) setFehler(err instanceof ApiError ? err.message : 'Statistik konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!abgebrochen) setLadend(false)
      })
    return () => {
      abgebrochen = true
    }
  }, [])

  return { daten, ladend, fehler }
}

export function useStatistikDrilldown(wettbewerbId: number | null, saisonId: number | null) {
  const [daten, setDaten] = useState<StatistikDrilldownDto | null>(null)
  const [ladend, setLadend] = useState(false)
  const [fehler, setFehler] = useState<string | null>(null)

  useEffect(() => {
    if (wettbewerbId === null || saisonId === null) {
      setDaten(null)
      return
    }
    let abgebrochen = false
    setLadend(true)
    setFehler(null)
    api
      .getTrefferquoteDrilldown(wettbewerbId, saisonId)
      .then((res) => {
        if (!abgebrochen) setDaten(res)
      })
      .catch((err: unknown) => {
        if (!abgebrochen) setFehler(err instanceof ApiError ? err.message : 'Spieltag-Auswertung konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!abgebrochen) setLadend(false)
      })
    return () => {
      abgebrochen = true
    }
  }, [wettbewerbId, saisonId])

  return { daten, ladend, fehler }
}

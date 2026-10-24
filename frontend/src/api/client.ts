import type {
  LigaTabelleDto,
  SpielDetailDto,
  StatistikDrilldownDto,
  StatistikWettbewerbDto,
  WochenFeedDto,
} from '../types/api'

/**
 * Zentraler API-Fehler mit dem tatsaechlichen Server-Text, wo vorhanden - siehe CLAUDE.md
 * "Error messages: specific, not generic". Das Backend liefert bei Fehlern ein { error: string }
 * -Objekt (siehe SpieleController/TabelleController), das hier bevorzugt als Meldung verwendet
 * wird statt eines generischen "Request fehlgeschlagen".
 */
export class ApiError extends Error {
  readonly status?: number

  constructor(message: string, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string): Promise<T> {
  let res: Response
  try {
    res = await fetch(`/api${path}`)
  } catch {
    throw new ApiError('Server nicht erreichbar - läuft der Dev-Stack?')
  }

  if (!res.ok) {
    let serverMessage: string | undefined
    try {
      const body = (await res.json()) as { error?: string }
      serverMessage = body.error
    } catch {
      // Kein JSON-Body vorhanden - Fallback unten greift.
    }
    throw new ApiError(serverMessage ?? `Anfrage fehlgeschlagen (HTTP ${res.status})`, res.status)
  }

  return (await res.json()) as T
}

export const api = {
  getWoche: (jahr: number, kw: number, wettbewerbIds: number[]) => {
    const params = new URLSearchParams({ jahr: String(jahr), kw: String(kw) })
    for (const id of wettbewerbIds) params.append('wettbewerbId', String(id))
    return request<WochenFeedDto>(`/spiele/woche?${params.toString()}`)
  },

  getSpielDetail: (id: number) => request<SpielDetailDto>(`/spiele/${id}`),

  getTabelle: (wettbewerbId: number) => request<LigaTabelleDto>(`/tabelle/${wettbewerbId}`),

  getTrefferquoteUebersicht: () => request<StatistikWettbewerbDto[]>('/statistik/trefferquote'),

  getTrefferquoteDrilldown: (wettbewerbId: number, saisonId: number) =>
    request<StatistikDrilldownDto>(`/statistik/trefferquote/${wettbewerbId}/saison/${saisonId}`),
}

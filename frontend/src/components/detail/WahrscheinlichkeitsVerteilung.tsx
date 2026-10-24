import type { ErgebnisWahrscheinlichkeitDto } from '../../types/api'

interface WahrscheinlichkeitsVerteilungProps {
  verteilung: ErgebnisWahrscheinlichkeitDto[]
}

/** Vollstaendige Wahrscheinlichkeitsverteilung ueber alle Ergebnisse (nicht nur Top 3-4 wie in der Karte), siehe APP_STRUCTURE.md Abschnitt 2 und PREDICTION_FACTORS.md "Ausgabeform". */
export function WahrscheinlichkeitsVerteilung({ verteilung }: WahrscheinlichkeitsVerteilungProps) {
  const sortiert = [...verteilung].sort((a, b) => Number(b.wahrscheinlichkeit) - Number(a.wahrscheinlichkeit))
  const maxWert = sortiert.length > 0 ? Math.max(...sortiert.map((e) => Number(e.wahrscheinlichkeit))) : 1

  return (
    <div className="flex flex-col gap-1.5">
      {sortiert.map((eintrag, index) => {
        const prozent = Number(eintrag.wahrscheinlichkeit) * 100
        const breite = maxWert > 0 ? (Number(eintrag.wahrscheinlichkeit) / maxWert) * 100 : 0
        return (
          <div key={`${eintrag.heim}-${eintrag.auswaerts}`} className="flex items-center gap-2 text-xs">
            <span className={`w-10 shrink-0 font-semibold tabular-nums ${index === 0 ? 'text-accent' : 'text-text'}`}>
              {eintrag.heim}:{eintrag.auswaerts}
            </span>
            <div className="h-2 flex-1 overflow-hidden rounded-sm bg-bg-inset">
              <div
                className={`h-full rounded-sm ${index === 0 ? 'bg-accent' : 'bg-border-strong'}`}
                style={{ width: `${breite}%` }}
              />
            </div>
            <span className="w-12 shrink-0 text-right tabular-nums text-text-muted">{prozent.toFixed(1)}%</span>
          </div>
        )
      })}
    </div>
  )
}

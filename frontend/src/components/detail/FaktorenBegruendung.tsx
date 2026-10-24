/**
 * Faktoren-Begruendung aus dem FaktorenSnapshot (APP_STRUCTURE.md Abschnitt 2: "welche Faktoren
 * wie stark eingeflossen sind"). Backend liefert das Feld bewusst typlos als JSON-Objekt (siehe
 * VorhersageDetailDto.FaktorenSnapshot: object) - das konkrete Schema ist noch nicht festgelegt,
 * da das Vorhersagemodell selbst laut PREDICTION_FACTORS.md erst in Stufen gebaut wird. Diese
 * Komponente rendert das Objekt deshalb generisch (Key/Value bzw. verschachtelt), statt ein festes
 * Schema anzunehmen - sobald das Backend ein konkretes Snapshot-Format liefert, kann hier eine
 * spezifischere Darstellung (z.B. pro Faktor mit Gewichtung/Prioritaet) nachgezogen werden.
 */
export function FaktorenBegruendung({ snapshot }: { snapshot: unknown }) {
  if (snapshot === null || snapshot === undefined) {
    return <p className="text-xs text-text-subtle">Keine Faktoren-Begründung verfügbar.</p>
  }

  if (typeof snapshot !== 'object') {
    return <p className="text-xs text-text-muted">{String(snapshot)}</p>
  }

  const eintraege = Object.entries(snapshot as Record<string, unknown>)
  if (eintraege.length === 0) {
    return <p className="text-xs text-text-subtle">Keine Faktoren-Begründung verfügbar.</p>
  }

  return (
    <dl className="flex flex-col gap-1.5 text-xs">
      {eintraege.map(([key, value]) => (
        <div key={key} className="flex items-start justify-between gap-3 border-b border-border pb-1.5 last:border-b-0">
          <dt className="text-text-muted">{key}</dt>
          <dd className="text-right font-medium text-text">
            {typeof value === 'object' ? JSON.stringify(value) : String(value)}
          </dd>
        </div>
      ))}
    </dl>
  )
}

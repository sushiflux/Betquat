const LABELS: Record<string, { text: string; className: string }> = {
  S: { text: 'S', className: 'bg-success-soft text-success' },
  U: { text: 'U', className: 'bg-bg-inset text-text-muted' },
  N: { text: 'N', className: 'bg-danger-soft text-danger' },
}

/** Formkurve als kompakte Symbolreihe (S/U/N), APP_STRUCTURE.md Abschnitt 2: "letzte 5-10 Spiele". */
export function Formkurve({ label, kurve }: { label: string; kurve: string[] }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="text-xs text-text-muted">{label}</span>
      <div className="flex gap-1">
        {kurve.length === 0 && <span className="text-xs text-text-subtle">–</span>}
        {kurve.map((ergebnis, i) => {
          const info = LABELS[ergebnis] ?? { text: '?', className: 'bg-bg-inset text-text-subtle' }
          return (
            <span
              key={i}
              className={`flex h-5 w-5 items-center justify-center rounded-sm text-[10px] font-bold ${info.className}`}
            >
              {info.text}
            </span>
          )
        })}
      </div>
    </div>
  )
}

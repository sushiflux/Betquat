import { Icon } from '@iconify/react'
import type { KwRef } from '../../utils/kalenderwoche'
import { formatKwZeitraum } from '../../utils/kalenderwoche'

interface WochenNavigationProps {
  kw: KwRef
  onVor: () => void
  onZurueck: () => void
  onHeute: () => void
  istAktuelleWoche: boolean
}

export function WochenNavigation({ kw, onVor, onZurueck, onHeute, istAktuelleWoche }: WochenNavigationProps) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-md border border-border bg-bg-elevated px-3 py-2">
      <button
        type="button"
        onClick={onZurueck}
        aria-label="Vorherige Kalenderwoche"
        className="flex h-8 w-8 items-center justify-center rounded-sm text-text-muted transition-colors hover:bg-bg-inset hover:text-text"
      >
        <Icon icon="tabler:chevron-left" width={18} height={18} />
      </button>

      <div className="flex flex-col items-center">
        <span className="text-sm font-semibold text-text">KW {kw.kw}</span>
        <span className="text-xs text-text-subtle">{formatKwZeitraum(kw)}</span>
      </div>

      <div className="flex items-center gap-1">
        {!istAktuelleWoche && (
          <button
            type="button"
            onClick={onHeute}
            className="mr-1 rounded-sm border border-border px-2 py-1 text-xs font-medium text-text-muted transition-colors hover:border-border-strong hover:text-text"
          >
            Heute
          </button>
        )}
        <button
          type="button"
          onClick={onVor}
          aria-label="Nächste Kalenderwoche"
          className="flex h-8 w-8 items-center justify-center rounded-sm text-text-muted transition-colors hover:bg-bg-inset hover:text-text"
        >
          <Icon icon="tabler:chevron-right" width={18} height={18} />
        </button>
      </div>
    </div>
  )
}

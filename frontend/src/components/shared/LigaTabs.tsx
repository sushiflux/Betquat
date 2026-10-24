import { motion } from 'motion/react'
import { LAENDER } from '../../config/wettbewerbe'

interface LigaTabsProps {
  landKey: string
  ligaKey: string
  onSelectLand: (key: string) => void
  onSelectLiga: (key: string) => void
}

/** Land-Tabs (oberste Ebene) + Liga-Unter-Tabs, gemaess APP_STRUCTURE.md "Grundprinzip". */
export function LigaTabs({ landKey, ligaKey, onSelectLand, onSelectLiga }: LigaTabsProps) {
  const aktivesLand = LAENDER.find((l) => l.key === landKey) ?? LAENDER[0]

  return (
    <div className="flex flex-col gap-3">
      <div className="flex gap-2" role="tablist" aria-label="Land">
        {LAENDER.map((land) => (
          <button
            key={land.key}
            type="button"
            role="tab"
            aria-selected={land.key === landKey}
            onClick={() => onSelectLand(land.key)}
            className={`relative rounded-sm px-4 py-2 text-sm font-semibold transition-colors ${
              land.key === landKey ? 'text-accent-text' : 'text-text-muted hover:text-text'
            }`}
          >
            {land.key === landKey && (
              <motion.span
                layoutId="land-tab-highlight"
                className="absolute inset-0 rounded-sm bg-accent"
                transition={{ type: 'spring', stiffness: 500, damping: 35 }}
              />
            )}
            <span className="relative flex items-center gap-2">
              <span aria-hidden="true">{land.flag}</span>
              {land.label}
            </span>
          </button>
        ))}
      </div>

      {aktivesLand.ligen.length > 1 && (
        <div className="flex gap-1 border-b border-border" role="tablist" aria-label="Liga">
          {aktivesLand.ligen.map((liga) => (
            <button
              key={liga.key}
              type="button"
              role="tab"
              aria-selected={liga.key === ligaKey}
              onClick={() => onSelectLiga(liga.key)}
              className="relative px-3 py-2 text-sm font-medium text-text-muted transition-colors hover:text-text data-[active=true]:text-accent"
              data-active={liga.key === ligaKey}
            >
              {liga.label}
              {liga.key === ligaKey && (
                <motion.span
                  layoutId="liga-tab-underline"
                  className="absolute inset-x-0 -bottom-px h-0.5 bg-accent"
                  transition={{ type: 'spring', stiffness: 500, damping: 35 }}
                />
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

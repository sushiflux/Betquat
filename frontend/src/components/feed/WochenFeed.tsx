import { AnimatePresence, motion } from 'motion/react'
import { useState } from 'react'
import { useWochenFeed } from '../../hooks/useWochenFeed'
import { FehlerHinweis, LadeHinweis, LeererZustand } from '../shared/StatusHinweis'
import type { KwRef } from '../../utils/kalenderwoche'
import { addWeeks, formatTagMonat, formatWochentagLang, getCurrentKw } from '../../utils/kalenderwoche'
import { sortiereSpieleDesTages } from '../../utils/spielSortierung'
import { SpielKarte } from './SpielKarte'
import { WochenNavigation } from './WochenNavigation'
import { SpielDetailModal } from '../detail/SpielDetailModal'

interface WochenFeedProps {
  wettbewerbIds: number[]
  ligaLabel: string
}

export function WochenFeed({ wettbewerbIds, ligaLabel }: WochenFeedProps) {
  const [kw, setKw] = useState<KwRef>(getCurrentKw)
  const [offenesSpielId, setOffenesSpielId] = useState<number | null>(null)
  const { daten, ladend, fehler } = useWochenFeed(kw, wettbewerbIds)
  const aktuelleKw = getCurrentKw()
  const istAktuelleWoche = kw.jahr === aktuelleKw.jahr && kw.kw === aktuelleKw.kw

  return (
    <div className="flex flex-col gap-4">
      <WochenNavigation
        kw={kw}
        onVor={() => setKw((k) => addWeeks(k, 1))}
        onZurueck={() => setKw((k) => addWeeks(k, -1))}
        onHeute={() => setKw(aktuelleKw)}
        istAktuelleWoche={istAktuelleWoche}
      />

      {ladend && <LadeHinweis text={`Lade Spiele der ${ligaLabel}…`} />}
      {fehler && <FehlerHinweis text={fehler} />}

      {!ladend && !fehler && daten && daten.tage.length === 0 && (
        <LeererZustand text={`Keine Spiele der ${ligaLabel} in dieser Kalenderwoche (z. B. Länderspielpause).`} />
      )}

      {!ladend && !fehler && daten && daten.tage.length > 0 && (
        <div className="flex flex-col gap-5">
          <AnimatePresence mode="popLayout">
            {daten.tage.map((tag) => (
              <motion.div
                key={tag.datum}
                layout
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className="flex flex-col gap-2"
              >
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-text">{formatWochentagLang(tag.datum)}</span>
                  <span className="text-xs text-text-subtle">{formatTagMonat(tag.datum)}</span>
                  <div className="h-px flex-1 bg-border" />
                </div>
                <div className="flex flex-col gap-2">
                  {sortiereSpieleDesTages(tag.spiele).map((spiel) => (
                    <SpielKarte key={spiel.id} spiel={spiel} onOeffnen={setOffenesSpielId} />
                  ))}
                </div>
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}

      <SpielDetailModal spielId={offenesSpielId} onClose={() => setOffenesSpielId(null)} />
    </div>
  )
}

import { Icon } from '@iconify/react'
import { AnimatePresence, motion } from 'motion/react'
import { useEffect } from 'react'
import { useSpielDetail } from '../../hooks/useSpielDetail'
import { formatUhrzeit } from '../../utils/kalenderwoche'
import { FehlerHinweis, LadeHinweis } from '../shared/StatusHinweis'
import { TeamLogo } from '../shared/TeamLogo'
import { FaktorenBegruendung } from './FaktorenBegruendung'
import { Formkurve } from './Formkurve'
import { WahrscheinlichkeitsVerteilung } from './WahrscheinlichkeitsVerteilung'

interface SpielDetailModalProps {
  spielId: number | null
  onClose: () => void
}

function formatDatum(iso: string): string {
  return new Date(iso).toLocaleDateString('de-DE', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function SpielDetailModal({ spielId, onClose }: SpielDetailModalProps) {
  const { daten, ladend, fehler } = useSpielDetail(spielId)

  useEffect(() => {
    if (spielId === null) return
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    document.body.style.overflow = 'hidden'
    return () => {
      window.removeEventListener('keydown', onKeyDown)
      document.body.style.overflow = ''
    }
  }, [spielId, onClose])

  return (
    <AnimatePresence>
      {spielId !== null && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={onClose}
          role="presentation"
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.97, y: 8 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.97, y: 8 }}
            transition={{ type: 'spring', stiffness: 400, damping: 32 }}
            onClick={(e) => e.stopPropagation()}
            role="dialog"
            aria-modal="true"
            aria-label="Spiel-Detail"
            className="flex max-h-[85vh] w-full max-w-2xl flex-col overflow-hidden rounded-lg border border-border bg-bg-elevated shadow-xl"
          >
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
              <span className="text-sm font-semibold text-text">
                {daten ? `${daten.wettbewerbName} · ${daten.spieltagName ?? `${daten.spieltagNummer}. Spieltag`}` : 'Spiel-Detail'}
              </span>
              <button
                type="button"
                onClick={onClose}
                aria-label="Schließen"
                className="flex h-8 w-8 items-center justify-center rounded-sm text-text-muted transition-colors hover:bg-bg-inset hover:text-text"
              >
                <Icon icon="tabler:x" width={18} height={18} />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto px-4 py-4">
              {ladend && <LadeHinweis text="Lade Spiel-Detail…" />}
              {fehler && <FehlerHinweis text={fehler} />}

              {!ladend && !fehler && daten && (
                <div className="flex flex-col gap-5">
                  <div className="flex flex-col items-center gap-2 text-center">
                    <span className="text-xs text-text-subtle">
                      {formatDatum(daten.anstoss)} · {formatUhrzeit(daten.anstoss)} Uhr
                    </span>
                    <div className="flex w-full items-center justify-center gap-4">
                      <div className="flex flex-1 flex-col items-center gap-1.5">
                        <TeamLogo url={daten.heimTeam.logoUrl} name={daten.heimTeam.kurzname ?? daten.heimTeam.name} size="lg" />
                        <span className="text-sm font-medium text-text">{daten.heimTeam.name}</span>
                      </div>
                      <div className="flex flex-col items-center gap-1">
                        <span className="text-2xl font-bold tabular-nums text-text">
                          {daten.status === 'geplant' ? '– : –' : `${daten.toreHeim} : ${daten.toreAuswaerts}`}
                        </span>
                        {daten.halbzeitToreHeim !== null && (
                          <span className="text-xs text-text-subtle">
                            HZ {daten.halbzeitToreHeim}:{daten.halbzeitToreAuswaerts}
                          </span>
                        )}
                        {daten.status === 'live' && <span className="text-xs font-semibold text-live">Live {daten.liveMinute}'</span>}
                      </div>
                      <div className="flex flex-1 flex-col items-center gap-1.5">
                        <TeamLogo url={daten.auswaertsTeam.logoUrl} name={daten.auswaertsTeam.kurzname ?? daten.auswaertsTeam.name} size="lg" />
                        <span className="text-sm font-medium text-text">{daten.auswaertsTeam.name}</span>
                      </div>
                    </div>
                    {daten.radioLink && (daten.radioLink.einzelspielUrl ?? daten.radioLink.konferenzUrl) && (
                      <a
                        href={daten.radioLink.einzelspielUrl ?? daten.radioLink.konferenzUrl ?? undefined}
                        target="_blank"
                        rel="noreferrer"
                        className="mt-1 inline-flex items-center gap-1.5 rounded-sm border border-border px-2.5 py-1 text-xs font-medium text-text-muted transition-colors hover:border-border-strong hover:text-accent"
                      >
                        <Icon icon="tabler:radio" width={14} height={14} />
                        {daten.radioLink.einzelspielUrl ? 'Einzelspiel-Stream' : 'Konferenz-Stream'}
                      </a>
                    )}
                  </div>

                  {daten.torEvents.length > 0 && (
                    <section className="flex flex-col gap-1.5">
                      <h3 className="text-xs font-semibold uppercase tracking-wide text-text-subtle">Torschützen</h3>
                      <ul className="flex flex-col gap-1">
                        {daten.torEvents.map((ev, i) => (
                          <li key={i} className="flex items-center justify-between text-xs">
                            <span className="text-text">
                              {ev.spielerName ?? 'Unbekannt'}
                              {ev.istElfmeter && <span className="text-text-subtle"> (Elfmeter)</span>}
                              {ev.istEigentor && <span className="text-text-subtle"> (Eigentor)</span>}
                            </span>
                            <span className="tabular-nums text-text-muted">
                              {ev.minute}' · {ev.toreHeimNachEvent}:{ev.toreAuswaertsNachEvent}
                            </span>
                          </li>
                        ))}
                      </ul>
                    </section>
                  )}

                  <section className="flex flex-col gap-1.5">
                    <h3 className="text-xs font-semibold uppercase tracking-wide text-text-subtle">Formkurve (letzte 5 Spiele)</h3>
                    <div className="flex flex-col gap-1.5 rounded-md bg-bg-inset p-2.5">
                      <Formkurve label={daten.heimTeam.name} kurve={daten.formkurveHeim} />
                      <Formkurve label={daten.auswaertsTeam.name} kurve={daten.formkurveAuswaerts} />
                    </div>
                  </section>

                  {daten.vorhersage ? (
                    <>
                      <section className="flex flex-col gap-1.5">
                        <h3 className="text-xs font-semibold uppercase tracking-wide text-text-subtle">
                          Wahrscheinlichkeitsverteilung
                        </h3>
                        <WahrscheinlichkeitsVerteilung verteilung={daten.vorhersage.verteilung} />
                      </section>

                      <section className="flex flex-col gap-1.5">
                        <h3 className="text-xs font-semibold uppercase tracking-wide text-text-subtle">Faktoren-Begründung</h3>
                        <FaktorenBegruendung snapshot={daten.vorhersage.faktorenSnapshot} />
                      </section>
                    </>
                  ) : (
                    <p className="rounded-sm bg-bg-inset px-3 py-2 text-xs text-text-subtle">
                      Für dieses Spiel liegt keine Vorhersage vor (z. B. weil es vor Beginn unserer Tipp-Historie
                      stattfand).
                    </p>
                  )}

                  {daten.headToHead.length > 0 && (
                    <section className="flex flex-col gap-1.5">
                      <h3 className="text-xs font-semibold uppercase tracking-wide text-text-subtle">Head-to-Head</h3>
                      <ul className="flex flex-col gap-1">
                        {daten.headToHead.map((h2h, i) => (
                          <li key={i} className="flex items-center justify-between text-xs">
                            <span className="text-text-muted">
                              {h2h.heimTeamName} – {h2h.auswaertsTeamName}
                            </span>
                            <span className="tabular-nums text-text">
                              {h2h.toreHeim ?? '–'}:{h2h.toreAuswaerts ?? '–'}
                            </span>
                          </li>
                        ))}
                      </ul>
                    </section>
                  )}
                </div>
              )}
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}

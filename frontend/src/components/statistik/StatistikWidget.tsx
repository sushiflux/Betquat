import { Icon } from '@iconify/react'
import { AnimatePresence, motion } from 'motion/react'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { useStatistikDrilldown, useStatistikUebersicht } from '../../hooks/useStatistik'
import type { StatistikWettbewerbDto, TrefferquoteZeileDto } from '../../types/api'
import { FehlerHinweis, LadeHinweis } from '../shared/StatusHinweis'

interface StatistikWidgetProps {
  wettbewerbId: number
  ligaLabel: string
}

/** Kompakte Kennzahlen-Kachel (APP_STRUCTURE.md Abschnitt 3: "Kachel-Grundansicht"). */
function Kachel({
  titel,
  unterzeile,
  children,
}: {
  titel: string
  unterzeile?: string | null
  children: ReactNode
}) {
  return (
    <div className="flex flex-1 flex-col items-center gap-1 rounded-md border border-border bg-bg-inset/60 px-2 py-3 text-center">
      <span className="text-[10px] font-semibold uppercase tracking-wide text-text-subtle">{titel}</span>
      {children}
      {unterzeile && <span className="text-[10px] text-text-subtle">{unterzeile}</span>}
    </div>
  )
}

function KachelWert({ zeile, art }: { zeile: TrefferquoteZeileDto | null; art: 'exakt' | 'tendenz' }) {
  if (!zeile || zeile.anzahlSpiele === 0) {
    return <span className="text-sm font-medium text-text-subtle">–</span>
  }
  const quote = art === 'exakt' ? zeile.exakteTrefferquote : zeile.tendenzTrefferquote
  return <span className="text-xl font-bold tabular-nums text-text">{(Number(quote) * 100).toFixed(0)} %</span>
}

function SaisonKachelWert({ zeile }: { zeile: TrefferquoteZeileDto | null }) {
  if (!zeile || zeile.anzahlSpiele === 0) {
    return <span className="text-sm font-medium text-text-subtle">Noch keine Spiele</span>
  }
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-sm font-semibold tabular-nums text-text">
        {zeile.anzahlExakteTreffer} von {zeile.anzahlSpiele} exakt
      </span>
      <span className="text-xs tabular-nums text-text-muted">
        {zeile.anzahlTendenzTreffer} von {zeile.anzahlSpiele} Tendenz
      </span>
    </div>
  )
}

function Quote({ zeile }: { zeile: TrefferquoteZeileDto | null }) {
  if (!zeile || zeile.anzahlSpiele === 0) {
    return <span className="text-xs text-text-subtle">Noch keine ausgewerteten Spiele</span>
  }
  return (
    <div className="flex items-center gap-3 text-xs">
      <span className="text-text-muted">
        Exakt <span className="font-semibold text-text">{(Number(zeile.exakteTrefferquote) * 100).toFixed(1)}%</span>
      </span>
      <span className="text-text-muted">
        Tendenz <span className="font-semibold text-text">{(Number(zeile.tendenzTrefferquote) * 100).toFixed(1)}%</span>
      </span>
      <span className="text-text-subtle">({zeile.anzahlSpiele} Sp.)</span>
    </div>
  )
}

function DrilldownSaisonZeile({ wettbewerbId, saison }: { wettbewerbId: number; saison: TrefferquoteZeileDto }) {
  const [offen, setOffen] = useState(false)
  const { daten, ladend, fehler } = useStatistikDrilldown(offen ? wettbewerbId : null, offen ? saison.saisonId : null)

  return (
    <div className="flex flex-col gap-1">
      <button
        type="button"
        onClick={() => setOffen((o) => !o)}
        className="flex items-center justify-between gap-2 rounded-sm px-1.5 py-1 text-left transition-colors hover:bg-bg-inset"
      >
        <span className="flex items-center gap-1.5 text-xs font-medium text-text">
          <Icon icon="tabler:chevron-right" width={12} height={12} className={`transition-transform ${offen ? 'rotate-90' : ''}`} />
          {saison.saisonName}
        </span>
        <Quote zeile={saison} />
      </button>
      <AnimatePresence>
        {offen && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            className="ml-4 overflow-hidden border-l border-border pl-3"
          >
            {ladend && <LadeHinweis text="Lade Spieltage…" />}
            {fehler && <FehlerHinweis text={fehler} />}
            {!ladend && !fehler && daten && daten.spieltage.length === 0 && (
              <p className="py-1 text-xs text-text-subtle">Noch keine ausgewerteten Spieltage in dieser Saison.</p>
            )}
            {!ladend &&
              !fehler &&
              daten?.spieltage.map((sp) => (
                <div key={sp.spieltagNummer} className="flex items-center justify-between gap-2 py-1">
                  <span className="text-xs text-text-muted">{sp.spieltagNummer}. Spieltag</span>
                  <Quote zeile={sp} />
                </div>
              ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

/**
 * Statistik-Widget "Wie gut lag ich richtig?" (APP_STRUCTURE.md Abschnitt 3) - Aenderung
 * ggue. der urspruenglichen Umsetzung (Nutzer-Korrektur): jetzt durch den aktiven Land-/Liga-Tab
 * GEFILTERT statt liga-uebergreifend, genau wie Wochen-Feed und Live-Tabelle. Grundansicht als
 * Kacheln statt Liste; die volle Drill-down-Ansicht (Spieltag-fuer-Spieltag) bleibt ausklappbar.
 */
export function StatistikWidget({ wettbewerbId, ligaLabel }: StatistikWidgetProps) {
  const { daten, ladend, fehler } = useStatistikUebersicht()
  const [drilldownOffen, setDrilldownOffen] = useState(false)

  const wettbewerb: StatistikWettbewerbDto | undefined = daten?.find((w) => w.wettbewerbId === wettbewerbId)

  // "Aktueller/letzter Spieltag" = die juengste Saison dieses Wettbewerbs (saisons ist
  // Backend-seitig absteigend nach Startjahr sortiert, siehe StatistikController), deren
  // Spieltag-Drilldown geladen wird um daraus die Zeile mit der hoechsten SpieltagNummer zu
  // nehmen (= zuletzt abgeschlossener Spieltag, da Eintraege erst nach Spieltag-Abschluss
  // entstehen - siehe TrefferquoteService).
  const aktuelleSaison = wettbewerb?.saisons[0] ?? null
  const { daten: aktuelleSaisonDrilldown } = useStatistikDrilldown(
    aktuelleSaison ? wettbewerbId : null,
    aktuelleSaison?.saisonId ?? null,
  )
  const letzterSpieltag = aktuelleSaisonDrilldown?.spieltage.at(-1) ?? null

  return (
    <div className="flex flex-col gap-3 rounded-md border border-border bg-bg-elevated p-3">
      <div className="flex flex-col gap-0.5">
        <h2 className="text-sm font-semibold text-text">Trefferquote · {ligaLabel}</h2>
        <p className="text-xs text-text-subtle">Wie gut lagen unsere Tipps bisher? Aktualisiert nach jedem abgeschlossenen Spieltag.</p>
      </div>

      {ladend && <LadeHinweis text="Lade Statistik…" />}
      {fehler && <FehlerHinweis text={fehler} />}

      {!ladend && !fehler && wettbewerb && (
        <>
          <div className="flex gap-2">
            <Kachel titel="Exaktes Ergebnis" unterzeile={letzterSpieltag ? `${letzterSpieltag.spieltagNummer}. Spieltag` : 'Aktueller Spieltag'}>
              <KachelWert zeile={letzterSpieltag} art="exakt" />
            </Kachel>
            <Kachel titel="Tendenz 1X2" unterzeile={letzterSpieltag ? `${letzterSpieltag.spieltagNummer}. Spieltag` : 'Aktueller Spieltag'}>
              <KachelWert zeile={letzterSpieltag} art="tendenz" />
            </Kachel>
            <Kachel titel="Aktuelle Saison" unterzeile={aktuelleSaison?.saisonName}>
              <SaisonKachelWert zeile={aktuelleSaison} />
            </Kachel>
          </div>

          <button
            type="button"
            onClick={() => setDrilldownOffen((o) => !o)}
            className="flex items-center justify-center gap-1.5 rounded-sm py-1 text-xs font-medium text-text-muted transition-colors hover:text-text"
          >
            <Icon icon="tabler:chevron-down" width={14} height={14} className={`transition-transform ${drilldownOffen ? 'rotate-180' : ''}`} />
            {drilldownOffen ? 'Detailansicht ausblenden' : 'Alle Saisons & Spieltage anzeigen'}
          </button>

          <AnimatePresence>
            {drilldownOffen && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: 'auto', opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                className="flex flex-col gap-1 overflow-hidden border-t border-border pt-2"
              >
                <div className="flex items-center justify-between gap-2 px-1.5 py-1">
                  <span className="text-xs font-semibold text-text">Gesamt</span>
                  <Quote zeile={wettbewerb.gesamt} />
                </div>
                {wettbewerb.saisons.length === 0 && <p className="py-1 text-xs text-text-subtle">Keine Saison-Daten vorhanden.</p>}
                {wettbewerb.saisons.map((saison) => (
                  <DrilldownSaisonZeile key={saison.saisonId} wettbewerbId={wettbewerbId} saison={saison} />
                ))}
              </motion.div>
            )}
          </AnimatePresence>
        </>
      )}
    </div>
  )
}

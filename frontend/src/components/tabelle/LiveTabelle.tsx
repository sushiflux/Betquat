import { Icon } from '@iconify/react'
import { motion } from 'motion/react'
import { findZone, zoneColor, zonenFuerLiga, zoneRowClass } from '../../config/tabellenzonen'
import { useTabelle } from '../../hooks/useTabelle'
import type { TabellenZeileDto } from '../../types/api'
import { FehlerHinweis, LadeHinweis, LeererZustand } from '../shared/StatusHinweis'
import { TeamLogo } from '../shared/TeamLogo'

interface LiveTabelleProps {
  wettbewerbId: number
  ligaLabel: string
}

const FORM_STYLES: Record<string, string> = {
  S: 'bg-success-soft text-success',
  U: 'bg-bg-inset text-text-muted',
  N: 'bg-danger-soft text-danger',
}

function FormReihe({ form }: { form: string[] }) {
  if (form.length === 0) {
    return <span className="text-xs text-text-subtle">–</span>
  }
  return (
    <div className="flex justify-end gap-0.5">
      {form.map((ergebnis, i) => (
        <span
          key={i}
          className={`flex h-4 w-4 items-center justify-center rounded-[2px] text-[9px] font-bold ${FORM_STYLES[ergebnis] ?? 'bg-bg-inset text-text-subtle'}`}
          title={ergebnis === 'S' ? 'Sieg' : ergebnis === 'N' ? 'Niederlage' : 'Unentschieden'}
        >
          {ergebnis}
        </span>
      ))}
    </div>
  )
}

/** Trend-Anzeige (APP_STRUCTURE.md Abschnitt 1b: "3. ▲ (5.)") - Vergleich zur Platzierung nach dem vorletzten abgeschlossenen Spieltag. */
function TrendAnzeige({ platz, vorherigerPlatz }: { platz: number; vorherigerPlatz: number | null }) {
  if (vorherigerPlatz === null) {
    return null
  }
  if (vorherigerPlatz === platz) {
    return <span className="text-[10px] text-text-subtle">({vorherigerPlatz}.)</span>
  }
  const verbessert = vorherigerPlatz > platz
  return (
    <span
      className={`inline-flex items-center gap-0.5 text-[10px] font-semibold ${verbessert ? 'text-success' : 'text-danger'}`}
      title={`Vorheriger Spieltag: Platz ${vorherigerPlatz}`}
    >
      <Icon icon={verbessert ? 'tabler:triangle-filled' : 'tabler:triangle-inverted-filled'} width={8} height={8} />
      <span className="text-text-subtle">({vorherigerPlatz}.)</span>
    </span>
  )
}

/** Auszeichnungs-Icons neben dem Teamnamen (APP_STRUCTURE.md Abschnitt 1b "Auszeichnungs-Icons"): Meisterschale (Titelverteidiger), DFB-Pokal (aktueller Pokalsieger, nur 1./2. Bundesliga), "A" fuer Aufsteiger (nur 1./2. Bundesliga). */
function AuszeichnungsIcons({ zeile }: { zeile: TabellenZeileDto }) {
  if (!zeile.istTitelverteidiger && !zeile.istPokalsieger && !zeile.istAufsteiger) {
    return null
  }
  return (
    <span className="flex shrink-0 items-center gap-1">
      {zeile.istTitelverteidiger && (
        <span className="text-[#c9a227]" title="Titelverteidiger (Meister der Vorsaison)">
          <Icon icon="tabler:trophy-filled" width={13} height={13} />
        </span>
      )}
      {zeile.istPokalsieger && (
        <span className="text-text-muted" title="Aktueller DFB-Pokalsieger">
          <Icon icon="tabler:award-filled" width={13} height={13} />
        </span>
      )}
      {zeile.istAufsteiger && (
        <span
          className="flex h-3.5 w-3.5 items-center justify-center rounded-[2px] bg-success-soft text-[9px] font-bold text-success"
          title="Aufsteiger dieser Saison"
        >
          A
        </span>
      )}
    </span>
  )
}

function TabellenZeile({ zeile, wettbewerbId }: { zeile: TabellenZeileDto; wettbewerbId: number }) {
  const zone = findZone(wettbewerbId, zeile.platz)

  return (
    <motion.tr
      key={zeile.teamId}
      layout
      transition={{ type: 'spring', stiffness: 350, damping: 30 }}
      className={`${zoneRowClass(zone)} ${zone ? '' : 'bg-bg-inset/60'}`}
      title={zone?.label}
    >
      <td className="py-1 pl-2">
        <div className="flex items-center gap-1">
          <span className="text-text-muted tabular-nums">{zeile.platz}</span>
          <TrendAnzeige platz={zeile.platz} vorherigerPlatz={zeile.vorherigerPlatz} />
        </div>
      </td>
      <td className="py-1 pl-1">
        <div className="flex items-center gap-1.5">
          <TeamLogo url={zeile.teamLogoUrl} name={zeile.teamKurzname ?? zeile.teamName} size="sm" />
          <span className="truncate font-medium text-text">{zeile.teamKurzname ?? zeile.teamName}</span>
          <AuszeichnungsIcons zeile={zeile} />
        </div>
      </td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.spiele}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.siege}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.unentschieden}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.niederlagen}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.toreGeschossen}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">{zeile.toreKassiert}</td>
      <td className="py-1 pl-2 text-right tabular-nums text-text-muted">
        {zeile.tordifferenz > 0 ? `+${zeile.tordifferenz}` : zeile.tordifferenz}
      </td>
      <td className="py-1 pl-2 text-right font-semibold tabular-nums text-text">{zeile.punkte}</td>
      <td className="py-1 pl-2 pr-2">
        <FormReihe form={zeile.formLetzte5} />
      </td>
    </motion.tr>
  )
}

/** Legende der Farbzonen unterhalb der Tabelle (APP_STRUCTURE.md Abschnitt 1b "Legende") - nur die Zonen, die diese Liga tatsaechlich hat. */
function ZonenLegende({ wettbewerbId }: { wettbewerbId: number }) {
  const zonen = zonenFuerLiga(wettbewerbId)
  if (zonen.length === 0) {
    return null
  }
  return (
    <div className="flex flex-wrap gap-x-3 gap-y-1 border-t border-border pt-2 text-[10px] text-text-muted">
      {zonen.map((zone) => (
        <span key={zone.typ} className="flex items-center gap-1">
          <span className="h-2 w-2 rounded-[1px]" style={{ backgroundColor: zoneColor(zone.typ) }} aria-hidden="true" />
          {zone.label}
        </span>
      ))}
    </div>
  )
}

export function LiveTabelle({ wettbewerbId, ligaLabel }: LiveTabelleProps) {
  const { daten, ladend, fehler } = useTabelle(wettbewerbId)

  return (
    <div className="flex flex-col gap-2 rounded-md border border-border bg-bg-elevated p-3">
      <h2 className="text-sm font-semibold text-text">Tabelle · {ligaLabel}</h2>

      {ladend && <LadeHinweis text="Lade Tabelle…" />}
      {fehler && <FehlerHinweis text={fehler} />}

      {!ladend && !fehler && daten && daten.tabelle.length === 0 && (
        <LeererZustand text="Noch keine Tabellendaten für diesen Wettbewerb (Saisonstart steht noch aus)." />
      )}

      {!ladend && !fehler && daten && daten.tabelle.length > 0 && (
        <div className="flex flex-col gap-2">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[480px] border-separate border-spacing-y-0.5 text-xs">
              <thead>
                <tr className="text-left text-[11px] uppercase tracking-wide text-text-subtle">
                  <th className="py-1 pl-2 font-medium">#</th>
                  <th className="py-1 pl-1 font-medium">Team</th>
                  <th className="py-1 pl-2 text-right font-medium" title="Spiele">
                    Sp
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Siege">
                    S
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Unentschieden">
                    U
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Niederlagen">
                    N
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Tore">
                    Tore
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Gegentore">
                    Geg.
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Tordifferenz">
                    +/-
                  </th>
                  <th className="py-1 pl-2 text-right font-medium" title="Punkte">
                    Pkt
                  </th>
                  <th className="py-1 pl-2 pr-2 text-right font-medium" title="Form der letzten 5 Spiele">
                    Form
                  </th>
                </tr>
              </thead>
              <tbody>
                {daten.tabelle.map((zeile) => (
                  <TabellenZeile key={zeile.teamId} zeile={zeile} wettbewerbId={wettbewerbId} />
                ))}
              </tbody>
            </table>
          </div>
          <ZonenLegende wettbewerbId={wettbewerbId} />
        </div>
      )}
    </div>
  )
}

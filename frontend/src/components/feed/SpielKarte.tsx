import { Icon } from '@iconify/react'
import { motion } from 'motion/react'
import type { SpielListeDto } from '../../types/api'
import { exaktquoteFarbklasse } from '../../utils/exaktquoteFarbe'
import { formatUhrzeit } from '../../utils/kalenderwoche'
import { TeamLogo } from '../shared/TeamLogo'

interface SpielKarteProps {
  spiel: SpielListeDto
  onOeffnen: (id: number) => void
}

function ErgebnisAnzeige({ spiel }: { spiel: SpielListeDto }) {
  if (spiel.status === 'geplant') {
    return <span className="text-lg font-semibold text-text-subtle">– : –</span>
  }
  return (
    <span className="text-lg font-bold tabular-nums text-text">
      {spiel.toreHeim} : {spiel.toreAuswaerts}
    </span>
  )
}

function StatusZeile({ spiel }: { spiel: SpielListeDto }) {
  if (spiel.status === 'geplant') {
    return <span className="text-xs text-text-muted">Anstoß: {formatUhrzeit(spiel.anstoss)} Uhr</span>
  }
  if (spiel.status === 'live') {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-live">
        <span className="relative flex h-1.5 w-1.5">
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-live opacity-75" />
          <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-live" />
        </span>
        Live {spiel.liveMinute}'
      </span>
    )
  }
  if (spiel.status === 'abgesagt') {
    return <span className="text-xs font-medium text-danger">Abgesagt</span>
  }
  return <span className="text-xs text-text-muted">Beendet</span>
}

function TippZeile({ spiel }: { spiel: SpielListeDto }) {
  const { vorhersage } = spiel

  if (!vorhersage) {
    return <span className="text-xs text-text-subtle">Keine Vorhersage verfügbar</span>
  }

  const trefferIcon =
    spiel.status === 'beendet' && vorhersage.trafExakt !== null ? (
      <span
        className={`inline-flex items-center gap-1 rounded-sm px-1.5 py-0.5 text-xs font-semibold ${
          vorhersage.trafExakt ? 'bg-success-soft text-success' : 'bg-danger-soft text-danger'
        }`}
        title={vorhersage.trafExakt ? 'Tipp war exakt richtig' : vorhersage.trafTendenz ? 'Tendenz richtig, Ergebnis nicht exakt' : 'Tipp lag daneben'}
      >
        <Icon icon={vorhersage.trafExakt ? 'tabler:check' : 'tabler:x'} width={13} height={13} />
        {vorhersage.trafExakt ? 'Treffer!' : vorhersage.trafTendenz ? 'Tendenz ✓' : ''}
      </span>
    ) : null

  const exaktWahrscheinlichkeit = Number(vorhersage.tippWahrscheinlichkeit)

  return (
    <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
      <span className="font-semibold text-text">
        Tipp: {vorhersage.tippToreHeim}:{vorhersage.tippToreAuswaerts}
      </span>
      {/* Farblich abgestuft nach der Hoehe der Exakt-Ergebnis-Wahrscheinlichkeit (PREDICTION_FACTORS.md
          "Farbliche Abstufung der Exakt-Ergebnis-Prozentzahl") - eigenes, kontinuierliches Signal auf
          nur dieser Zahl, getrennt vom binaeren Konfidenz-Badge weiter unten auf der Karte (das
          basiert auf der 1X2-Tendenz, nicht auf dieser Exakt-Ergebnis-Zahl). */}
      <span className={`font-semibold ${exaktquoteFarbklasse(exaktWahrscheinlichkeit)}`} title="Wahrscheinlichkeit für genau dieses exakte Ergebnis">
        ({(exaktWahrscheinlichkeit * 100).toFixed(0)} %)
      </span>
      <span className="text-text-subtle">
        · Heim {(Number(vorhersage.wahrscheinlichkeitHeimsieg) * 100).toFixed(0)} % · Remis{' '}
        {(Number(vorhersage.wahrscheinlichkeitRemis) * 100).toFixed(0)} % · Ausw{' '}
        {(Number(vorhersage.wahrscheinlichkeitAuswaertssieg) * 100).toFixed(0)} %
      </span>
      {trefferIcon}
    </div>
  )
}

export function SpielKarte({ spiel, onOeffnen }: SpielKarteProps) {
  const hoheKonfidenz = spiel.vorhersage?.istHoheKonfidenz ?? false

  return (
    <motion.button
      type="button"
      onClick={() => onOeffnen(spiel.id)}
      layout
      initial={{ opacity: 0, y: 6 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2 }}
      className={`group relative flex w-full flex-col gap-2 rounded-md border bg-bg-elevated p-3 text-left transition-colors hover:border-border-strong ${
        hoheKonfidenz ? 'border-confidence/60 shadow-[0_0_0_1px_var(--c-confidence)]' : 'border-border'
      }`}
      aria-label={`Details zu ${spiel.heimTeam.name} gegen ${spiel.auswaertsTeam.name} anzeigen`}
    >
      {spiel.hatRadioLink && (
        <span
          className="absolute right-3 top-3 text-text-subtle group-hover:text-accent"
          title="Radioübertragung verfügbar"
        >
          <Icon icon="tabler:radio" width={16} height={16} />
        </span>
      )}

      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <TeamLogo url={spiel.heimTeam.logoUrl} name={spiel.heimTeam.kurzname ?? spiel.heimTeam.name} />
          <span className="truncate text-sm font-medium text-text">{spiel.heimTeam.name}</span>
        </div>

        <ErgebnisAnzeige spiel={spiel} />

        <div className="flex min-w-0 flex-1 items-center justify-end gap-2 text-right">
          <span className="truncate text-sm font-medium text-text">{spiel.auswaertsTeam.name}</span>
          <TeamLogo url={spiel.auswaertsTeam.logoUrl} name={spiel.auswaertsTeam.kurzname ?? spiel.auswaertsTeam.name} />
        </div>
      </div>

      <div className={`flex flex-col gap-1 border-t border-border pt-2 ${hoheKonfidenz ? 'pb-5' : ''}`}>
        <StatusZeile spiel={spiel} />
        <TippZeile spiel={spiel} />
      </div>

      {/* Konfidenz-Badge unten rechts (Bugfix: sass urspruenglich oben rechts und ueberdeckte dort
          das Team-Logo, siehe APP_STRUCTURE.md Abschnitt 1 "Positions-Korrektur"). */}
      {hoheKonfidenz && (
        <span
          className="absolute bottom-3 right-3 flex items-center gap-1 rounded-sm bg-confidence-soft px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-confidence"
          title="Hohe Modell-Konfidenz: unsere Vorhersage ist bei diesem Spiel ungewöhnlich eindeutig"
        >
          <Icon icon="tabler:target-arrow" width={12} height={12} />
          Hohe Konfidenz
        </span>
      )}
    </motion.button>
  )
}

import type { SpielListeDto } from '../types/api'

/**
 * Dynamische Sortierung innerhalb eines Wochentag-Blocks (APP_STRUCTURE.md Abschnitt 1:
 * "aktuell laufende Spiele stehen immer oben ... Prioritaet: live > noch nicht angepfiffen
 * (chronologisch) > beendet"). Betrifft nur die Reihenfolge INNERHALB eines Tages - die
 * Wochentag-Gruppierung selbst bleibt chronologisch und wird hier nicht angefasst.
 *
 * "Abgesagt" wird wie "beendet" (nachrangig) behandelt - ein abgesagtes Spiel ist fuer den
 * Nutzer ebenfalls "erledigt", nicht mehr relevant fuer die oberste Sichtbarkeit.
 */
const STATUS_PRIORITAET: Record<SpielListeDto['status'], number> = {
  live: 0,
  geplant: 1,
  beendet: 2,
  abgesagt: 2,
}

export function sortiereSpieleDesTages(spiele: SpielListeDto[]): SpielListeDto[] {
  return [...spiele].sort((a, b) => {
    const prioA = STATUS_PRIORITAET[a.status]
    const prioB = STATUS_PRIORITAET[b.status]
    if (prioA !== prioB) return prioA - prioB
    // Innerhalb derselben Prioritaetsstufe chronologisch nach Anstosszeit (deckt sowohl
    // "geplant" (Anstoss steht noch bevor) als auch eine sinnvolle Tie-Break-Reihenfolge fuer
    // mehrere gleichzeitig laufende/beendete Spiele ab).
    return new Date(a.anstoss).getTime() - new Date(b.anstoss).getTime()
  })
}

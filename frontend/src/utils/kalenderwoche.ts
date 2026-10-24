/** ISO-8601-Kalenderwochen-Berechnung (Montag als Wochenstart, KW 1 = Woche mit erstem Donnerstag des Jahres) - passend zum Backend, das Spiele nach KwJahr/KwNummer gruppiert (siehe SpielDtos.cs). */
export interface KwRef {
  jahr: number
  kw: number
}

export function getIsoWeek(date: Date): KwRef {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  // Montag = 1 ... Sonntag = 7
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  const weekNo = Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7)
  return { jahr: d.getUTCFullYear(), kw: weekNo }
}

export function getCurrentKw(): KwRef {
  return getIsoWeek(new Date())
}

/** Anzahl ISO-Kalenderwochen eines Jahres (52 oder 53). */
function weeksInYear(jahr: number): number {
  const p = (y: number) => {
    const d = new Date(Date.UTC(y, 0, 1))
    const dow = d.getUTCDay() || 7
    return dow === 4 || (dow === 3 && isLeapYear(y))
  }
  return p(jahr) || p(jahr - 1) ? 53 : 52
}

function isLeapYear(y: number): boolean {
  return (y % 4 === 0 && y % 100 !== 0) || y % 400 === 0
}

export function addWeeks(ref: KwRef, delta: number): KwRef {
  let { jahr, kw } = ref
  kw += delta
  while (kw < 1) {
    jahr -= 1
    kw += weeksInYear(jahr)
  }
  while (kw > weeksInYear(jahr)) {
    kw -= weeksInYear(jahr)
    jahr += 1
  }
  return { jahr, kw }
}

/** Montag-Datum einer ISO-Kalenderwoche, fuer die Anzeige "KW 38 (18.09. - 24.09.)". */
export function getMondayOfIsoWeek(ref: KwRef): Date {
  const jan4 = new Date(Date.UTC(ref.jahr, 0, 4))
  const jan4Day = jan4.getUTCDay() || 7
  const week1Monday = new Date(jan4)
  week1Monday.setUTCDate(jan4.getUTCDate() - jan4Day + 1)
  const target = new Date(week1Monday)
  target.setUTCDate(week1Monday.getUTCDate() + (ref.kw - 1) * 7)
  return target
}

const WOCHENTAG_LABELS = ['Sonntag', 'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag']

export function formatWochentagLang(datumIso: string): string {
  const d = new Date(`${datumIso}T00:00:00`)
  return WOCHENTAG_LABELS[d.getDay()]
}

export function formatTagMonat(datumIso: string): string {
  const d = new Date(`${datumIso}T00:00:00`)
  return d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })
}

export function formatKwZeitraum(ref: KwRef): string {
  const montag = getMondayOfIsoWeek(ref)
  const sonntag = new Date(montag)
  sonntag.setUTCDate(montag.getUTCDate() + 6)
  const fmt = (d: Date) => d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })
  return `${fmt(montag)} – ${fmt(sonntag)}`
}

export function formatUhrzeit(isoDateTime: string): string {
  return new Date(isoDateTime).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })
}

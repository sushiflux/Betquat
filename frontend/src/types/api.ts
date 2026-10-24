/**
 * TypeScript-Spiegel der Backend-DTOs (backend/Controllers/Dtos/*.cs). Feldnamen 1:1 aus dem
 * .NET-JSON uebernommen (camelCase durch ASP.NET Cores Default-Serialisierung), damit hier keine
 * Mapping-Schicht noetig ist. Bei Aenderung der Backend-DTOs muss diese Datei manuell nachgezogen
 * werden - es gibt (noch) keine automatische Codegen-Pipeline zwischen Backend und Frontend.
 */

export interface TeamDto {
  id: number
  name: string
  kurzname: string | null
  logoUrl: string | null
}

export interface VorhersageKompaktDto {
  tippToreHeim: number
  tippToreAuswaerts: number
  tippWahrscheinlichkeit: number
  wahrscheinlichkeitHeimsieg: number
  wahrscheinlichkeitRemis: number
  wahrscheinlichkeitAuswaertssieg: number
  trafExakt: boolean | null
  trafTendenz: boolean | null
  istHoheKonfidenz: boolean
  konfidenzScore: number
}

export type SpielStatus = 'geplant' | 'live' | 'beendet' | 'abgesagt'

export interface SpielListeDto {
  id: number
  wettbewerbId: number
  wettbewerbName: string
  wettbewerbKurzname: string
  spieltagNummer: number
  spieltagName: string | null
  anstoss: string
  kwJahr: number
  kwNummer: number
  heimTeam: TeamDto
  auswaertsTeam: TeamDto
  status: SpielStatus
  liveMinute: number | null
  toreHeim: number | null
  toreAuswaerts: number | null
  vorhersage: VorhersageKompaktDto | null
  hatRadioLink: boolean
}

export interface WochenFeedTagDto {
  datum: string
  spiele: SpielListeDto[]
}

export interface WochenFeedDto {
  kwJahr: number
  kwNummer: number
  tage: WochenFeedTagDto[]
}

export interface TorEventDto {
  teamId: number
  spielerName: string | null
  minute: number
  istElfmeter: boolean
  istEigentor: boolean
  toreHeimNachEvent: number
  toreAuswaertsNachEvent: number
}

export interface ErgebnisWahrscheinlichkeitDto {
  heim: number
  auswaerts: number
  wahrscheinlichkeit: number
}

export interface VorhersageDetailDto {
  tippToreHeim: number
  tippToreAuswaerts: number
  tippWahrscheinlichkeit: number
  wahrscheinlichkeitHeimsieg: number
  wahrscheinlichkeitRemis: number
  wahrscheinlichkeitAuswaertssieg: number
  wahrscheinlichkeitUeber25: number | null
  wahrscheinlichkeitBeideTreffen: number | null
  verteilung: ErgebnisWahrscheinlichkeitDto[]
  faktorenSnapshot: unknown
  trafExakt: boolean | null
  trafTendenz: boolean | null
  berechnetAm: string
  istHoheKonfidenz: boolean
  konfidenzScore: number
}

export interface HeadToHeadEintragDto {
  anstoss: string
  heimTeamName: string
  auswaertsTeamName: string
  toreHeim: number | null
  toreAuswaerts: number | null
}

export interface RadioLinkDto {
  einzelspielUrl: string | null
  konferenzUrl: string | null
}

export interface SpielDetailDto {
  id: number
  wettbewerbId: number
  wettbewerbName: string
  spieltagNummer: number
  spieltagName: string | null
  anstoss: string
  heimTeam: TeamDto
  auswaertsTeam: TeamDto
  status: SpielStatus
  liveMinute: number | null
  toreHeim: number | null
  toreAuswaerts: number | null
  halbzeitToreHeim: number | null
  halbzeitToreAuswaerts: number | null
  torEvents: TorEventDto[]
  vorhersage: VorhersageDetailDto | null
  headToHead: HeadToHeadEintragDto[]
  formkurveHeim: string[]
  formkurveAuswaerts: string[]
  radioLink: RadioLinkDto | null
}

export interface TabellenZeileDto {
  platz: number
  teamId: number
  teamName: string
  teamKurzname: string | null
  teamLogoUrl: string | null
  spiele: number
  siege: number
  unentschieden: number
  niederlagen: number
  toreGeschossen: number
  toreKassiert: number
  tordifferenz: number
  punkte: number
  /** Form der letzten 5 Saison-Spiele, "S"/"U"/"N", aeltestes zuerst. */
  formLetzte5: string[]
  /** Tabellenplatz nach dem vorletzten abgeschlossenen Spieltag, fuer die Trend-Anzeige. Null wenn kein "vorher" existiert (erster Spieltag). */
  vorherigerPlatz: number | null
  /** True wenn dieses Team Titelverteidiger dieser Liga ist (Meisterschale-Icon). */
  istTitelverteidiger: boolean
  /** True wenn dieses Team aktueller DFB-Pokalsieger ist (nur 1./2. Bundesliga). */
  istPokalsieger: boolean
  /** True wenn dieses Team in der aktuellen Saison aufgestiegen ist (nur 1./2. Bundesliga). */
  istAufsteiger: boolean
}

export interface LigaTabelleDto {
  wettbewerbId: number
  wettbewerbName: string
  istPokal: boolean
  tabelle: TabellenZeileDto[]
}

export type TrefferquoteEbene = 'gesamt' | 'saison' | 'spieltag'

export interface TrefferquoteZeileDto {
  ebene: TrefferquoteEbene
  saisonId: number | null
  saisonName: string | null
  spieltagNummer: number | null
  anzahlSpiele: number
  anzahlExakteTreffer: number
  exakteTrefferquote: number
  anzahlTendenzTreffer: number
  tendenzTrefferquote: number
  berechnetAm: string
}

export interface StatistikWettbewerbDto {
  wettbewerbId: number
  wettbewerbName: string
  gesamt: TrefferquoteZeileDto | null
  saisons: TrefferquoteZeileDto[]
}

export interface StatistikDrilldownDto {
  wettbewerbId: number
  saisonId: number
  spieltage: TrefferquoteZeileDto[]
}

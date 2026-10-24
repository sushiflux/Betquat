import { branding } from './config/branding'
import { WochenFeed } from './components/feed/WochenFeed'
import { LiveTabelle } from './components/tabelle/LiveTabelle'
import { StatistikWidget } from './components/statistik/StatistikWidget'
import { LigaTabs } from './components/shared/LigaTabs'
import { ThemeToggle } from './components/shared/ThemeToggle'
import { useLigaAuswahl } from './hooks/useLigaAuswahl'
import { useTheme } from './hooks/useTheme'
import { useVersion } from './hooks/useVersion'

function App() {
  const { land, liga, selectLand, selectLiga } = useLigaAuswahl()
  const { theme, toggleTheme } = useTheme()
  const version = useVersion()

  return (
    <div className="min-h-screen bg-bg">
      <header className="border-b border-border bg-bg-elevated">
        <div className="flex flex-col gap-4 px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-baseline gap-2">
              <h1 className="text-lg font-bold tracking-tight text-text">{branding.name}</h1>
              <span className="hidden text-xs text-text-subtle sm:inline">{branding.tagline}</span>
            </div>
            <div className="flex items-center gap-3">
              {version && <span className="text-xs text-text-subtle">{version}</span>}
              <ThemeToggle theme={theme} onToggle={toggleTheme} />
            </div>
          </div>

          <LigaTabs landKey={land.key} ligaKey={liga.key} onSelectLand={selectLand} onSelectLiga={selectLiga} />
        </div>
      </header>

      <main className="px-4 py-6 sm:px-6 lg:px-8">
        {/* Spaltenverteilung (APP_STRUCTURE.md "Grundprinzip" -> "Layout-Breite"): volle Fensterbreite
            statt zentriertem Max-Width-Container, Live-Tabelle bekommt bewusst mehr Raum als der
            Wochen-Feed (schmalere Spiel-Karten-Spalte), da die Tabelle seit der Erweiterung auf die
            volle Spaltenzahl (Platz/Team/Sp/S/U/N/Tore/Gegentore/+-/Pkt/Form) deutlich mehr
            horizontalen Platz braucht, um ohne Scrollen lesbar zu bleiben. */}
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,0.8fr)_minmax(600px,1.3fr)] xl:grid-cols-[minmax(0,0.75fr)_minmax(620px,1.3fr)_minmax(280px,0.75fr)]">
          <section aria-label={`Wochen-Feed ${liga.label}`}>
            <WochenFeed wettbewerbIds={[liga.wettbewerbId]} ligaLabel={liga.label} />
          </section>

          <aside className="flex flex-col gap-6" aria-label="Live-Tabelle">
            <LiveTabelle wettbewerbId={liga.wettbewerbId} ligaLabel={liga.label} />
            {/* Statistik-Widget ist wie Wochen-Feed/Live-Tabelle durch den aktiven Land-/Liga-Tab gefiltert (APP_STRUCTURE.md Abschnitt 3) - auf schmalen/mittleren Breiten (< xl) landet es hier direkt unter der Tabelle in derselben Seitenspalte, ab xl in einer eigenen dritten Spalte. */}
            <div className="xl:hidden">
              <StatistikWidget wettbewerbId={liga.wettbewerbId} ligaLabel={liga.label} />
            </div>
          </aside>

          <aside className="hidden xl:flex xl:flex-col xl:gap-6" aria-label="Statistik">
            <StatistikWidget wettbewerbId={liga.wettbewerbId} ligaLabel={liga.label} />
          </aside>
        </div>
      </main>
    </div>
  )
}

export default App

/**
 * Einheitliche Team-Logo-Darstellung, verwendet ueberall wo ein Team-Logo auftaucht
 * (SpielKarte, LiveTabelle, SpielDetailModal) - siehe CLAUDE.md "UI consistency": eine
 * wiederkehrende visuelle Einheit bekommt eine einzige Implementierung statt mehrerer
 * Kopien mit leicht abweichendem Markup.
 *
 * Bug-Fix (Nutzer-Report): manche Vereins-Logos (z.B. SC Paderborn 07, siehe dessen SVG bei
 * Wikimedia Commons: reine Fuellfarben #0065A5/#1F1A17/#FFFFFF, kein Hintergrund-Chip im SVG
 * selbst) bestehen aus dunklen/schwarzen Konturen auf transparentem Hintergrund und werden im
 * Dark-Theme dadurch praktisch unsichtbar. Statt das als Einzelfall fuer Paderborn zu loesen
 * (naechstes dunkles Logo haette dasselbe Problem), bekommt JEDES Team-Logo denselben dezenten
 * hellen Kreis-Chip als Hintergrund - funktioniert unabhaengig davon, ob ein Logo dunkel, hell
 * oder gemischtfarbig ist, und haelt die Bildgroesse selbst unveraendert (kein CSS-Filter noetig,
 * der auch helle/bunte Logos verfaelschen wuerde).
 */
export function TeamLogo({
  url,
  name,
  size = 'md',
}: {
  url: string | null
  name: string
  size?: 'sm' | 'md' | 'lg'
}) {
  const dims = size === 'sm' ? 'h-4 w-4' : size === 'lg' ? 'h-12 w-12' : 'h-9 w-9'
  const padding = size === 'sm' ? 'p-0.5' : size === 'lg' ? 'p-1.5' : 'p-1'
  const textSize = size === 'sm' ? 'text-[8px]' : 'text-[10px]'

  if (!url) {
    return (
      <div
        className={`flex ${dims} shrink-0 items-center justify-center rounded-full bg-bg-inset ${textSize} font-semibold text-text-subtle`}
        aria-hidden="true"
      >
        {name.slice(0, 3).toUpperCase()}
      </div>
    )
  }

  return (
    <span className={`flex ${dims} shrink-0 items-center justify-center rounded-full bg-white ${padding} shadow-[inset_0_0_0_1px_rgba(0,0,0,0.06)]`}>
      <img src={url} alt="" className="h-full w-full object-contain" loading="lazy" />
    </span>
  )
}

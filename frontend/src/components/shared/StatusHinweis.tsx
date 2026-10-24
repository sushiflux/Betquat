import { Icon } from '@iconify/react'

export function LadeHinweis({ text = 'Lädt…' }: { text?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 rounded-md border border-border bg-bg-elevated px-4 py-8 text-sm text-text-muted">
      <Icon icon="tabler:loader-2" width={18} height={18} className="animate-spin" />
      {text}
    </div>
  )
}

export function FehlerHinweis({ text }: { text: string }) {
  return (
    <div className="flex items-start gap-2 rounded-md border border-danger/40 bg-danger-soft px-4 py-3 text-sm text-danger">
      <Icon icon="tabler:alert-circle" width={18} height={18} className="mt-0.5 shrink-0" />
      <span>{text}</span>
    </div>
  )
}

export function LeererZustand({ text }: { text: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-md border border-dashed border-border px-4 py-10 text-center text-sm text-text-subtle">
      <Icon icon="tabler:calendar-off" width={22} height={22} />
      {text}
    </div>
  )
}

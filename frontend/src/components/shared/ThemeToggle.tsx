import { Icon } from '@iconify/react'

interface ThemeToggleProps {
  theme: 'light' | 'dark'
  onToggle: () => void
}

export function ThemeToggle({ theme, onToggle }: ThemeToggleProps) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-label={theme === 'dark' ? 'Zu hellem Design wechseln' : 'Zu dunklem Design wechseln'}
      title={theme === 'dark' ? 'Helles Design' : 'Dunkles Design'}
      className="flex h-9 w-9 items-center justify-center rounded-sm border border-border bg-bg-elevated text-text-muted transition-colors hover:border-border-strong hover:text-text"
    >
      <Icon icon={theme === 'dark' ? 'tabler:sun' : 'tabler:moon'} width={18} height={18} />
    </button>
  )
}

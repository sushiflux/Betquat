import { useEffect, useState } from 'react'
import { branding } from '../config/branding'

interface VersionResponse {
  version: string
}

/** Zeigt die laufende Backend-Version klein/unaufdringlich an (siehe CLAUDE.md "Versioning scheme": "muss sichtbar sein, nicht nur ueber die API"). Einmaliger Abruf beim Mount, kein Polling - aendert sich nur bei Backend-Rebuild/-Restart. */
export function useVersion(): string | null {
  const [version, setVersion] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/version')
      .then((res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<VersionResponse>
      })
      .then((data) => setVersion(`${branding.name} ${data.version}`))
      .catch(() => {
        // Best-effort-Anzeige, kein nutzerinitiierter Vorgang - stilles Scheitern ist hier
        // bewusst in Ordnung (siehe CLAUDE.md "Error messages" Ausnahme fuer Hintergrund-Abrufe).
      })
  }, [])

  return version
}

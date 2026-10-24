# BetQuote - Versionsuebersicht

Schneller Ueberblick, was aktuell tatsaechlich im Einsatz ist. Keine Historie (dafuer sind die
Changelogs da) - wird bei jeder tatsaechlichen Versionsaenderung im selben Change aktualisiert,
und die Spalte "Latest available" wird zu Sessionbeginn live neu geprueft (siehe CLAUDE.md
"Versions overview file").

| Component | Current version | Latest available | Check for updates |
|---|---|---|---|
| .NET | 10.0.401 SDK / 10.0.12 Runtime | 10.0.401 SDK / 10.0.12 Runtime | https://dotnet.microsoft.com/download/dotnet |
| Node.js | 24 | 24 LTS | https://nodejs.org/en/download |
| React | 19.3.0 | 19.3.0 | https://github.com/facebook/react/releases |
| Vite | 8.3.0 | 8.3.0 | https://github.com/vitejs/vite/releases |
| TypeScript | 6.0.2 | 7.0.2 | https://github.com/microsoft/TypeScript/releases |
| Tailwind CSS | 4.3.3 | 4.3.3 | https://github.com/tailwindlabs/tailwindcss/releases |
| PostgreSQL | 18.6 | 18.6 | https://www.postgresql.org/docs/release/ |
| Caddy | 2 (alpine) | 2 | https://github.com/caddyserver/caddy/releases |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | 10.0.3 | https://github.com/npgsql/efcore.pg/releases |
| Microsoft.EntityFrameworkCore.Design | 10.0.12 | 10.0.12 | https://github.com/dotnet/efcore/releases |
| EFCore.NamingConventions | 10.0.1 | 10.0.1 | https://github.com/efcore/EFCore.NamingConventions/releases |
| HtmlAgilityPack | 1.13.0 | 1.13.0 | https://github.com/zzzprojects/html-agility-pack/releases |

**Hinweis TypeScript 7:** bewusst NICHT auf die frisch veroeffentlichte TypeScript-7-Reihe (der
native/Go-basierte Compiler-Rewrite, "tsgo") umgestellt, obwohl `npm view typescript version`
bereits 7.0.2 als "latest" liefert. Das `react-ts`-Vite-Template pinnt aktuell noch bewusst
`~6.0.2` - `@vitejs/plugin-react`/Vite 8 sind gegen TS 6.x gebaut/getestet, nicht nachweislich
gegen den TS-7-Rewrite. Ein Upgrade auf TS 7 waere ein echter Kompatibilitaets-Sprung (neue
Compiler-Architektur, nicht nur ein Patch), daher hier bewusst zurueckgestellt statt stillschweigend
erzwungen - siehe CLAUDE.md "Keep dependencies current" (Blocker-Ausnahme).

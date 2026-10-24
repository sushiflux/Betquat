import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true,
    port: 5192,
    strictPort: true,
    // Caddy reicht den echten Host-Header des Browsers durch (siehe CLAUDE.md:
    // "Local HTTPS via Caddy's built-in local CA") - alle Namen auflisten, unter denen
    // die Dev-Instanz erreichbar ist, sonst blockt Vites DNS-Rebinding-Schutz mit 403.
    allowedHosts: ['localhost', 'betquote.localhost'],
  },
})

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Aspire injects service URLs as:
//   services__server__http__0   → http://localhost:5534
//   services__server__https__0  → https://localhost:7491
// Fallback is the HTTP port from launchSettings.json
const serverUrl =
  process.env['services__server__http__0']  ||   // Aspire HTTP (preferred — no TLS issues)
  process.env['SERVER_HTTP']                ||   // legacy / manual
  process.env['services__server__https__0'] ||   // Aspire HTTPS fallback
  process.env['SERVER_HTTPS']               ||
  'http://localhost:5534';                        // launchSettings fallback

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: serverUrl,
        changeOrigin: true,
        secure: false,        // accept self-signed dev certs
        configure: (proxy) => {
          proxy.on('error', (err) => {
            console.error('[proxy error]', err.message, '→ target:', serverUrl);
          });
        },
      }
    }
  }
})

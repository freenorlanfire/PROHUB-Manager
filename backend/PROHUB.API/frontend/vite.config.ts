import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        // Aspire injects SERVER_HTTPS or SERVER_HTTP; prefer HTTP to avoid self-signed cert issues
        target: process.env.SERVER_HTTP || process.env.SERVER_HTTPS || 'http://localhost:5534',
        changeOrigin: true,
        secure: false,        // accept self-signed dev certs if HTTPS is used
      }
    }
  }
})

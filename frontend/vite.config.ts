import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Fail instead of silently moving to another port, so the README URL stays true.
    strictPort: true,
    // The browser calls relative /api/... URLs; Vite forwards them to the API.
    // Same origin for the browser, so no CORS setup is needed.
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})

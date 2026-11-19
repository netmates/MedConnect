import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // ApiGateway: чат и SignalR
      '/api/chats': {
        target: 'http://localhost:5070',
        changeOrigin: true,
        ws: true,
      },
      // ApiGateway: остальные запросы /api
      '/api': {
        target: 'http://localhost:5070',
        changeOrigin: true,
      },
    },
  },
})

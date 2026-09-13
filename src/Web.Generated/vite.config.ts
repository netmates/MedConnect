import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // CommunicationService (чат + SignalR hub)
      '/api/chats': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        ws: true,
      },
      // AppointmentService
      '/api': {
        target: 'http://localhost:5067',
        changeOrigin: true,
      },
    },
  },
})

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    allowedHosts: ['.serveousercontent.com', '.loca.lt'],
    proxy: {
      '/api': {
        target: 'http://localhost:5265',
        changeOrigin: true
      },
      '/photos': {
        target: 'http://localhost:5265',
        changeOrigin: true
      }
    }
  }
})
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/hubs': {
        target: 'http://localhost:5167',
        ws: true,
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5167',
        changeOrigin: true,
      },
    },
  },
})

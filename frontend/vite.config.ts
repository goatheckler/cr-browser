import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const backendTarget = process.env.BACKEND_URL || 'http://localhost:5214';
export default defineConfig({
  plugins: [sveltekit()],
  server: {
    proxy: {
      '/api': {
        target: backendTarget,
        changeOrigin: true
      }
    },
    host: '0.0.0.0',
    port: 5173
  }
});

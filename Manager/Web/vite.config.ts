import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5033',
        changeOrigin: true,
        secure: false,
      }
    }
  },
  build: {
    // Ant Design's runtime is ~900 KB minified / ~280 KB gzipped on its own and cannot be split
    // further without per-component dynamic imports, which would add network roundtrips for an
    // admin UI that ships nearly every component. 1000 KB is the realistic floor here.
    chunkSizeWarningLimit: 1000,
    rollupOptions: {
      output: {
        // Split heavy third-party libraries into their own chunks so they can be cached
        // independently of app code, and so the initial entry chunk stays under ~500 KB.
        manualChunks: (id) => {
          if (!id.includes('node_modules')) return undefined;
          if (id.includes('antd') || id.includes('rc-') || id.includes('@ant-design')) {
            return 'vendor-antd';
          }
          if (id.includes('react-router')) {
            return 'vendor-router';
          }
          if (id.includes('react-dom') || id.includes('/react/') || id.includes('scheduler')) {
            return 'vendor-react';
          }
          return 'vendor';
        },
      },
    },
  },
})

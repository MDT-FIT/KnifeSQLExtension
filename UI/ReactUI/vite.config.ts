import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { defineConfig } from 'vite';
/*import { viteSingleFile } from 'vite-plugin-singlefile';*/

// https://vite.dev/config/
export default defineConfig({
  base: './', 
  plugins: [tailwindcss(), react()],
  build: {
    outDir: 'dist',
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});

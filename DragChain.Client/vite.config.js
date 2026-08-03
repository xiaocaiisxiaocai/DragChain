import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import Icons from 'unplugin-icons/vite';
import { fileURLToPath, URL } from 'node:url';
export default defineConfig({
    plugins: [vue(), Icons({ compiler: 'vue3' })],
    build: {
        outDir: '../DragChain.API/wwwroot',
        emptyOutDir: false
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        port: 5173,
        proxy: {
            '/api': {
                target: 'http://localhost:5256',
                changeOrigin: true,
            },
            '/selection-result-images': {
                target: 'http://localhost:5256',
                changeOrigin: true,
            }
        }
    }
});

import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	optimizeDeps: {
		include: ['@microsoft/signalr']
	},
	server: {
		host: '0.0.0.0',
		port: 5173,
		strictPort: true
	}
});

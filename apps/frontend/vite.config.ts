import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
	const env = loadEnv(mode, '.', '');
	const apiProxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:8080';
	const apiProxyPrefix = env.VITE_API_PROXY_PREFIX || '/api';
	const apiProxyRewriteTo = env.VITE_API_PROXY_REWRITE_TO || apiProxyPrefix;

	return {
		plugins: [sveltekit()],
		server: {
			proxy: {
				[apiProxyPrefix]: {
					target: apiProxyTarget,
					changeOrigin: true,
					ws: true,
					rewrite: (path) => path.replace(new RegExp(`^${apiProxyPrefix}`), apiProxyRewriteTo)
				},
				'/auth': {
					target: apiProxyTarget,
					changeOrigin: false
				},
				'/signin-oidc': {
					target: apiProxyTarget,
					changeOrigin: false
				},
				'/signout-callback-oidc': {
					target: apiProxyTarget,
					changeOrigin: false
				},
				'/hubs': {
					target: apiProxyTarget,
					changeOrigin: true,
					ws: true
				}
			}
		}
	};
});

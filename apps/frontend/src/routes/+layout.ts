import { redirect } from '@sveltejs/kit';
import { buildBackendUrl, isAuthRedirectResponse, isJsonResponse, type AuthConfig } from '$lib/api';
import type { AuthUserContract } from '$lib/api';
import type { AuthUser } from '$lib/stores/auth';
import type { LayoutLoad } from './$types';

export const ssr = false;

const PUBLIC_ROUTES = new Set(['/login', '/callback']);
const JWT_TOKEN_KEY = 'konfigo_jwt_token';

export const load: LayoutLoad = async ({ url, fetch }) => {
	let authConfig: AuthConfig = { provider: 'openid' };

	try {
		const configResponse = await fetch(buildBackendUrl('/auth/config'));
		if (configResponse.ok) {
			authConfig = (await configResponse.json()) as AuthConfig;
		}
	} catch {
		// use default; connection error will surface from /auth/me
	}

	const meHeaders: HeadersInit = { Accept: 'application/json' };
	if (authConfig.provider === 'jwt') {
		const token = typeof localStorage !== 'undefined' ? localStorage.getItem(JWT_TOKEN_KEY) : null;
		if (token) {
			meHeaders['Authorization'] = `Bearer ${token}`;
		}
	}

	let response: Response;

	try {
		response = await fetch(buildBackendUrl('/auth/me'), {
			credentials: 'include',
			redirect: 'manual',
			headers: meHeaders
		});
	} catch {
		return {
			user: null,
			authConfig,
			connectionError: {
				message: 'The app could not reach the network or Konfigo backend. Check your connection and try again.'
			}
		};
	}

	let currentUser: AuthUser | null = null;

	if (isAuthRedirectResponse(response)) {
		currentUser = null;
	} else if (response.status === 401) {
		if (authConfig.provider === 'jwt' && typeof localStorage !== 'undefined') {
			localStorage.removeItem(JWT_TOKEN_KEY);
		}
		currentUser = null;
	} else if (response.status === 200 && isJsonResponse(response)) {
		const payload = (await response.json()) as AuthUserContract;
		if (!payload.id) {
			throw new Error('auth/me response missing id');
		}
		currentUser = {
			id: payload.id,
			email: payload.email ?? null,
			name: payload.name ?? null,
			roles: payload.roles ?? [],
			permissions: payload.permissions ?? []
		};
	} else if (response.status === 200) {
		currentUser = null;
	} else if (response.status !== 204) {
		return {
			user: null,
			authConfig,
			connectionError: {
				message: 'Konfigo backend is not available right now. Try again after the backend is running.',
				status: response.status
			}
		};
	}

	const isPublic = PUBLIC_ROUTES.has(url.pathname);

	if (!currentUser && !isPublic) {
		const returnUrl = url.pathname + url.search;
		throw redirect(302, `/login?returnUrl=${encodeURIComponent(returnUrl)}`);
	}

	if (currentUser && url.pathname === '/login') {
		const requested = url.searchParams.get('returnUrl');
		throw redirect(302, sanitizeReturnUrl(requested) ?? '/services');
	}

	return { user: currentUser, authConfig, connectionError: null };
};

function sanitizeReturnUrl(value: string | null): string | null {
	if (!value) return null;
	return value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\') ? value : null;
}

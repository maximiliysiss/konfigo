import { redirect } from '@sveltejs/kit';
import { buildBackendUrl } from '$lib/api';
import type { AuthUserContract } from '$lib/api';
import type { AuthUser } from '$lib/stores/auth';
import type { LayoutLoad } from './$types';

export const ssr = false;

const PUBLIC_ROUTES = new Set(['/login']);

export const load: LayoutLoad = async ({ url, fetch }) => {
	const response = await fetch(buildBackendUrl('/auth/me'), {
		credentials: 'include',
		headers: { Accept: 'application/json' }
	});

	let currentUser: AuthUser | null = null;

	if (response.status === 200) {
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
	} else if (response.status !== 204) {
		throw new Error(`auth/me returned ${response.status}`);
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

	return { user: currentUser };
};

function sanitizeReturnUrl(value: string | null): string | null {
	if (!value) return null;
	return value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\') ? value : null;
}

import { writable, get } from 'svelte/store';

export type AuthUser = {
	id: string;
	email: string | null;
	name: string | null;
	roles: string[];
	permissions: string[];
};

export type AuthProvider = 'openid' | 'saml' | 'jwt';

export const user = writable<AuthUser | null>(null);

const JWT_TOKEN_KEY = 'konfigo_jwt_token';

function createJwtTokenStore() {
	const initial = typeof localStorage !== 'undefined' ? localStorage.getItem(JWT_TOKEN_KEY) : null;
	const { subscribe, set: _set } = writable<string | null>(initial);

	function set(token: string | null) {
		if (typeof localStorage !== 'undefined') {
			if (token) {
				localStorage.setItem(JWT_TOKEN_KEY, token);
			} else {
				localStorage.removeItem(JWT_TOKEN_KEY);
			}
		}
		_set(token);
	}

	return { subscribe, set, get: () => get({ subscribe }) };
}

export const jwtToken = createJwtTokenStore();

export function canAll(currentUser: AuthUser | null): boolean {
	return !!currentUser?.permissions.includes('canAll');
}

export function canChange(currentUser: AuthUser | null): boolean {
	return canAll(currentUser) || !!currentUser?.permissions.includes('canChange');
}

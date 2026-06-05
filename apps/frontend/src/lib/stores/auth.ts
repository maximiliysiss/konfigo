import { writable } from 'svelte/store';

export type AuthUser = {
	id: string;
	email: string | null;
	name: string | null;
	roles: string[];
	permissions: string[];
};

export const user = writable<AuthUser | null>(null);

export function canAll(currentUser: AuthUser | null): boolean {
	return !!currentUser?.permissions.includes('canAll');
}

export function canChange(currentUser: AuthUser | null): boolean {
	return canAll(currentUser) || !!currentUser?.permissions.includes('canChange');
}

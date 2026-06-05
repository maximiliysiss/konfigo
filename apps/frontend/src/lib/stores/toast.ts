import { browser } from '$app/environment';
import { writable } from 'svelte/store';

export type AppToast = {
	id: number;
	message: string;
	type: 'success' | 'error' | 'info' | 'warning';
};

const TOAST_KEY = 'rcm.toast';
let seed = 1;

export const toasts = writable<AppToast[]>([]);

export function showToast(message: string, type: AppToast['type'] = 'info', ttlMs = 4000): void {
	const id = seed++;
	toasts.update((items) => [...items, { id, message, type }]);
	setTimeout(() => {
		toasts.update((items) => items.filter((x) => x.id !== id));
	}, ttlMs);
}

export function queueToast(message: string, type: AppToast['type'] = 'info'): void {
	if (!browser) {
		return;
	}
	localStorage.setItem(TOAST_KEY, JSON.stringify({ message, type }));
}

export function consumeQueuedToast(): void {
	if (!browser) {
		return;
	}

	const raw = localStorage.getItem(TOAST_KEY);
	if (!raw) {
		return;
	}

	localStorage.removeItem(TOAST_KEY);
	try {
		const parsed = JSON.parse(raw) as { message?: string; type?: AppToast['type'] };
		if (parsed.message) {
			showToast(parsed.message, parsed.type ?? 'info');
		}
	} catch {
		// ignore malformed payload
	}
}

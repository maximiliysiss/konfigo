import { writable } from 'svelte/store';

export type AvailabilityIssue = {
	message: string;
	status?: number;
};

export const authAvailabilityIssue = writable<AvailabilityIssue | null>(null);

export function showAuthUnavailable(status = 401): void {
	authAvailabilityIssue.set({
		message: 'Access to Konfigo is currently unavailable. Your session may have expired or the auth service is not responding.',
		status
	});
}

export function clearAuthUnavailable(): void {
	authAvailabilityIssue.set(null);
}

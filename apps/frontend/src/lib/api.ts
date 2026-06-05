import { PUBLIC_API_URL } from '$env/static/public';

const defaultApiBaseUrl = '/api';

export type PageResponse<T> = {
	entities: T[];
	nextPageToken: string | null;
};

export type AuthUserContract = {
	id?: string;
	email?: string | null;
	name?: string | null;
	roles?: string[] | null;
	permissions?: string[] | null;
};

export type ApplicationServiceContract = {
	id: string;
	num?: number;
	name: string;
	description: string | null;
	repositoryUrl: string | null;
	gitLabProjectId: string | null;
	contactEmail: string | null;
	createdAt: string;
	updatedAt: string | null;
};

export type ConfigVersionContract = {
	id: string;
	serviceId: string;
	versionLabel: string;
	description: string | null;
	createdAt: string;
	updatedAt: string | null;
};

export const CONFIG_VALUE_TYPE = {
	String: 0,
	DateTime: 1,
	TimeSpan: 2,
	Json: 3,
	Enum: 4,
	Number: 5,
	Boolean: 6,
	Array: 7
} as const;

export type ConfigValueType = (typeof CONFIG_VALUE_TYPE)[keyof typeof CONFIG_VALUE_TYPE];

export type ConfigEntryContract = {
	id: string;
	configVersionId: string;
	key: string;
	name: string;
	rawValue: string | null;
	valueType: ConfigValueType;
	enumDefinition: string | null;
	description: string | null;
	groupName: string | null;
	groupDescription: string | null;
	generation: number;
	createdAt: string;
	updatedAt: string | null;
};

export type AuditLogContract = {
	id: string;
	num?: number;
	serviceId: string;
	userId: string | null;
	entry: { type?: string | number; [key: string]: unknown };
	createdAt: string;
	updatedAt: string | null;
};

export function buildUrl(path: string): string {
	if (/^https?:\/\//.test(path)) {
		return path;
	}

	const base = (PUBLIC_API_URL?.trim() || defaultApiBaseUrl).replace(/\/+$/, '');
	const normalizedPath = path.startsWith('/') ? path : `/${path}`;
	return `${base}${normalizedPath}`;
}

export function buildBackendUrl(path: string): string {
	if (/^https?:\/\//.test(path)) {
		return path;
	}

	const apiBase = (PUBLIC_API_URL?.trim() || defaultApiBaseUrl).replace(/\/+$/, '');
	const backendBase = apiBase.endsWith('/api') ? apiBase.slice(0, -4) : apiBase;
	const normalizedPath = path.startsWith('/') ? path : `/${path}`;
	return `${backendBase}${normalizedPath}`;
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
	const headers = new Headers(init.headers ?? {});

	if (!headers.has('Content-Type') && init.body) {
		headers.set('Content-Type', 'application/json');
	}

	const response = await fetch(buildUrl(path), {
		credentials: 'include',
		...init,
		headers
	});

	if (response.status === 401) {
		const returnUrl = window.location.pathname + window.location.search;
		window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
		throw new Error('Unauthorized');
	}

	if (response.status === 403) {
		throw new Error('Forbidden');
	}

	if (!response.ok) {
		const text = await response.text();
		throw new Error(text || response.statusText || 'Request failed');
	}

	if (response.status === 204) {
		return undefined as T;
	}

	const text = await response.text();
	if (!text.trim()) {
		return undefined as T;
	}

	const contentType = response.headers.get('content-type') ?? '';
	if (contentType.includes('application/json')) {
		return JSON.parse(text) as T;
	}

	return text as T;
}

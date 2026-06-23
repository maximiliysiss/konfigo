<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { env } from '$env/dynamic/public';
	import { page } from '$app/stores';
	import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
	import { onMount } from 'svelte';
	import { apiRequest, buildBackendUrl, CONFIG_VALUE_TYPE, getApiErrorMessage } from '$lib/api';
	import type {
		ApplicationServiceContract,
		AuditLogContract,
		ConfigEntryContract,
		ConfigValueType,
		ConfigVersionContract,
		PageResponse
	} from '$lib/api';
	import { canAll, canChange, user } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Badge from '$lib/components/ui/Badge.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Textarea from '$lib/components/ui/Textarea.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import ConfirmDialog from '$lib/components/ui/ConfirmDialog.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import ErrorCallout from '../../../../../components/ui/ErrorCallout.svelte';

	type ConfigEntry = ConfigEntryContract;

	type EntryGroup = {
		key: string;
		title: string;
		description: string;
		entries: ConfigEntry[];
		isUngrouped: boolean;
		collapsed: boolean;
	};

	type VersionDetail = ConfigVersionContract;

	type ServiceDetail = ApplicationServiceContract;
	type MainTabKey = 'info' | 'members' | 'versions' | 'audit';
	type AuditEntry = AuditLogContract;
	type AuditDetail = { label: string; value: string };
	type AuditSummary = {
		type: string;
		title: string;
		subtitle: string;
		badge: 'default' | 'success' | 'warning' | 'danger' | 'accent';
		details: AuditDetail[];
		unknown: boolean;
	};

	type Toast = { id: number; message: string };
	type ConnectionStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';
	type ValueTypeOption = { label: string; value: ConfigValueType };

	const valueTypeOptions: ValueTypeOption[] = [
		{ label: 'String', value: CONFIG_VALUE_TYPE.String },
		{ label: 'Number', value: CONFIG_VALUE_TYPE.Number },
		{ label: 'Boolean', value: CONFIG_VALUE_TYPE.Boolean },
		{ label: 'DateTime', value: CONFIG_VALUE_TYPE.DateTime },
		{ label: 'TimeSpan', value: CONFIG_VALUE_TYPE.TimeSpan },
		{ label: 'Enum', value: CONFIG_VALUE_TYPE.Enum },
		{ label: 'Json', value: CONFIG_VALUE_TYPE.Json },
		{ label: 'Array', value: CONFIG_VALUE_TYPE.Array }
	];

	let service = $state<ServiceDetail | null>(null);
	let version = $state<VersionDetail | null>(null);
	let versions = $state<VersionDetail[]>([]);
	let entries = $state<ConfigEntry[]>([]);
	let mainTab = $state<MainTabKey>('versions');
	let loading = $state(true);
	let entriesLoading = $state(false);
	let auditLoading = $state(false);
	let error = $state('');
	let entriesError = $state('');
	let actionError = $state('');
	let formValueError = $state('');
	let inlineValueError = $state('');
	let pendingEntryErrors = $state<Record<string, string>>({});
	let audit = $state<AuditEntry[]>([]);
	let auditPage = $state(1);
	let auditPageSize = 10;
	let auditPageTokens = $state<string[]>(['']);
	let auditNextPageToken = $state('');
	let expandedRows = $state<Record<string, boolean>>({});

	let showPanel = $state(false);
	let panelMode = $state<'create' | 'edit'>('create');
	let saving = $state(false);
	let formId = $state('');
	let formKey = $state('');
	let formValueType = $state<ConfigValueType>(CONFIG_VALUE_TYPE.String);
	let formRawValue = $state('');
	let formArrayItems = $state<string[]>(['']);
	let formEnumDefinition = $state('');
	let formDescription = $state('');
	let formGroupName = $state('');
	let formGroupDescription = $state('');

	let inlineId = $state('');
	let inlineValue = $state('');
	let inlineArrayItems = $state<string[]>([]);
	let pendingValues = $state<Record<string, string>>({});
	let showOnlyChanged = $state(false);
	let batchSaving = $state(false);

	let showEdit = $state(false);
	let editName = $state('');
	let editDescription = $state('');
	let editRepositoryUrl = $state('');
	let editContactEmail = $state('');
	let savingService = $state(false);

	let showNewVersionModal = $state(false);
	let newVersionLabel = $state('');
	let newVersionDescription = $state('');
	let creatingVersion = $state(false);

	let confirmDeleteOpen = $state(false);
	let deleting = $state(false);
	let pendingDelete: ConfigEntry | null = $state(null);

	let toasts = $state<Toast[]>([]);
	let toastSeed = 1;
	let connection: HubConnection | null = null;
	let connectionStatus = $state<ConnectionStatus>('offline');
	let connectionServiceId = '';
	let connectionVersionId = '';
	let mounted = false;
	let activePageKey = '';
	let collapsedGroups = $state<Record<string, boolean>>({});
	let changedRows = $state<Record<string, number>>({});

	const serviceId = $derived($page.params.id ?? '');
	const versionId = $derived($page.params.versionId ?? '');
	const userCanAll = $derived(canAll($user));
	const userCanChange = $derived(canChange($user));
	const canSetExistingEntries = $derived(userCanChange);
	const canEditExistingEntries = $derived(userCanAll);
	const isMemberEditingExistingEntry = $derived(false);
	const pendingEntries = $derived(entries.filter((entry) => isEntryChanged(entry)));
	const pendingCount = $derived(pendingEntries.length);
	const canBatchSave = $derived(showOnlyChanged && pendingCount > 0 && !batchSaving);

	const knownGroups = $derived.by(() => {
		const groups = new Map<string, string>();
		for (const entry of entries) {
			const name = normalizedGroupName(entry.groupName);
			if (!name) continue;
			const description = entry.groupDescription?.trim() ?? '';
			if (!groups.has(name) || description) groups.set(name, description);
		}
		return Array.from(groups.entries())
			.map(([name, description]) => ({ name, description }))
			.sort((a, b) => a.name.localeCompare(b.name));
	});

	const selectedKnownGroup = $derived.by(() => {
		const normalized = normalizedGroupName(formGroupName);
		if (!normalized) return null;
		return knownGroups.find((group) => group.name === normalized) ?? null;
	});

	const isKnownGroupSelection = $derived(Boolean(selectedKnownGroup));

	const groupedEntries = $derived.by<EntryGroup[]>(() => {
		const buckets = new Map<string, EntryGroup>();

		for (const entry of entries) {
			if (showOnlyChanged && !isEntryChanged(entry)) continue;

			const groupName = normalizedGroupName(entry.groupName);
			const key = groupName || '__ungrouped__';
			const description = groupName
				? entry.groupDescription?.trim() || knownGroups.find((x) => x.name === groupName)?.description || ''
				: '';

			if (!buckets.has(key)) {
				buckets.set(key, {
					key,
					title: groupName || 'Ungrouped',
					description,
					entries: [],
					isUngrouped: !groupName,
					collapsed: groupName ? collapsedGroups[collapseStorageKey(groupName)] ?? false : false
				});
			}

			const bucket = buckets.get(key)!;
			if (!bucket.description && description) bucket.description = description;
			bucket.entries.push(entry);
		}

		return Array.from(buckets.values())
			.map((group) => ({ ...group, entries: [...group.entries].sort((a, b) => a.key.localeCompare(b.key)) }))
			.sort((a, b) => {
				if (a.isUngrouped) return -1;
				if (b.isUngrouped) return 1;
				return a.title.localeCompare(b.title);
			});
	});

	function normalizedGroupName(value?: string | null): string {
		return value?.trim() ?? '';
	}

	function versionTimestamp(version: VersionDetail): number {
		const parsed = new Date(version.createdAt).valueOf();
		return Number.isNaN(parsed) ? 0 : parsed;
	}

	function sortVersions(nextVersions: VersionDetail[]): VersionDetail[] {
		return [...nextVersions].sort((a, b) => versionTimestamp(b) - versionTimestamp(a));
	}

	function versionHref(nextVersionId: string): string {
		return `/services/${serviceId}/versions/${nextVersionId}`;
	}

	function collapseStorageKey(groupName: string): string {
		return `${serviceId}:${versionId}:${groupName}:collapsed`;
	}

	function loadCollapsedState() {
		if (!browser) return;
		const nextState: Record<string, boolean> = {};
		for (const group of knownGroups) {
			nextState[collapseStorageKey(group.name)] = localStorage.getItem(collapseStorageKey(group.name)) === 'true';
		}
		collapsedGroups = nextState;
	}

	function toggleGroup(groupName: string) {
		const storageKey = collapseStorageKey(groupName);
		const nextValue = !(collapsedGroups[storageKey] ?? false);
		collapsedGroups = { ...collapsedGroups, [storageKey]: nextValue };
		if (browser) localStorage.setItem(storageKey, String(nextValue));
	}

	function syncGroupDescription() {
		formGroupName = normalizedGroupName(formGroupName);
		const known = knownGroups.find((group) => group.name === formGroupName);
		if (known) formGroupDescription = known.description;
	}

	function valueTypeLabel(valueType: number | string): string {
		if (typeof valueType === 'string') return valueType;
		return valueTypeOptions.find((x) => x.value === valueType)?.label ?? 'Unknown';
	}

	function normalizeValueType(valueType: number | string): ConfigValueType {
		if (typeof valueType === 'number') {
			const found = valueTypeOptions.find((x) => x.value === valueType);
			return found?.value ?? CONFIG_VALUE_TYPE.String;
		}
		const found = valueTypeOptions.find((x) => x.label.toLowerCase() === valueType.toLowerCase());
		return found?.value ?? CONFIG_VALUE_TYPE.String;
	}

	function valueTypeBadge(valueType: number | string): 'default' | 'accent' | 'success' | 'warning' {
		const type = valueTypeLabel(valueType).toLowerCase();
		if (type === 'number') return 'accent';
		if (type === 'datetime' || type === 'timespan' || type === 'boolean') return 'success';
		if (type === 'json' || type === 'enum') return 'warning';
		return 'default';
	}

	function canInlineEdit(entry: ConfigEntry): boolean {
		if (!canSetExistingEntries) return false;
		const t = normalizeValueType(entry.valueType);
		return (
			t === CONFIG_VALUE_TYPE.String ||
			t === CONFIG_VALUE_TYPE.DateTime ||
			t === CONFIG_VALUE_TYPE.TimeSpan ||
			t === CONFIG_VALUE_TYPE.Json ||
			t === CONFIG_VALUE_TYPE.Array ||
			t === CONFIG_VALUE_TYPE.Number ||
			t === CONFIG_VALUE_TYPE.Boolean ||
			(t === CONFIG_VALUE_TYPE.Enum && entryEnumValues(entry).length > 0)
		);
	}

	function entryRawValue(entry: ConfigEntry): string {
		return entry.rawValue ?? '';
	}

	function getEntryValue(entry: ConfigEntry): string {
		return pendingValues[entry.id] ?? entryRawValue(entry);
	}

	function isEntryChanged(entry: ConfigEntry): boolean {
		return pendingValues[entry.id] !== undefined && pendingValues[entry.id] !== entryRawValue(entry);
	}

	function setPendingValue(entry: ConfigEntry, rawValue: string) {
		const normalized = String(rawValue ?? '');
		const original = entryRawValue(entry);
		const next = { ...pendingValues };
		const nextErrors = { ...pendingEntryErrors };

		if (normalized === original) delete next[entry.id];
		else next[entry.id] = normalized;
		delete nextErrors[entry.id];

		pendingValues = next;
		pendingEntryErrors = nextErrors;
	}

	function discardPendingChanges() {
		pendingValues = {};
		pendingEntryErrors = {};
		showOnlyChanged = false;
		cancelInlineEdit();
	}

	function isEnumValueType(valueType: number | string): boolean {
		return normalizeValueType(valueType) === CONFIG_VALUE_TYPE.Enum;
	}

	function isBooleanValueType(valueType: number | string): boolean {
		return normalizeValueType(valueType) === CONFIG_VALUE_TYPE.Boolean;
	}

	function isArrayValueType(valueType: number | string): boolean {
		return normalizeValueType(valueType) === CONFIG_VALUE_TYPE.Array;
	}

	function parseArrayItems(raw?: string | null): string[] {
		const normalized = String(raw ?? '').trim();
		if (!normalized) return [];

		try {
			const parsed = JSON.parse(normalized);
			if (!Array.isArray(parsed)) return [];
			return parsed.map((item) => {
				if (typeof item === 'string') return item;
				const serialized = JSON.stringify(item);
				return serialized === undefined ? String(item) : serialized;
			});
		} catch {
			return normalized
				.split('\n')
				.map((item) => item.trim())
				.filter(Boolean);
		}
	}

	function arrayItemsForDisplay(raw?: string | null): string[] {
		return parseArrayItems(raw);
	}

	function serializeArrayItems(items: string[]): string {
		return JSON.stringify(
			items.map((item) => {
				const trimmed = item.trim();
				if (!trimmed) return item;
				try {
					return JSON.parse(trimmed);
				} catch {
					return item;
				}
			})
		);
	}

	function addFormArrayItem() {
		formValueError = '';
		formArrayItems = [...formArrayItems, ''];
	}

	function updateFormArrayItem(index: number, value: string) {
		formValueError = '';
		formArrayItems = formArrayItems.map((item, itemIndex) => (itemIndex === index ? value : item));
	}

	function removeFormArrayItem(index: number) {
		formValueError = '';
		formArrayItems = formArrayItems.filter((_, itemIndex) => itemIndex !== index);
	}

	function addInlineArrayItem() {
		inlineValueError = '';
		inlineArrayItems = [...inlineArrayItems, ''];
	}

	function updateInlineArrayItem(index: number, value: string) {
		inlineValueError = '';
		inlineArrayItems = inlineArrayItems.map((item, itemIndex) => (itemIndex === index ? value : item));
	}

	function removeInlineArrayItem(index: number) {
		inlineValueError = '';
		inlineArrayItems = inlineArrayItems.filter((_, itemIndex) => itemIndex !== index);
	}

	function splitEnumValues(enumDefinition?: string | null): string[] {
		return (enumDefinition ?? '')
			.split(',')
			.map((x) => x.trim())
			.filter(Boolean);
	}

	function entryEnumValues(entry: ConfigEntry): string[] {
		return splitEnumValues(entry.enumDefinition);
	}

	function enumValues(): string[] {
		return splitEnumValues(formEnumDefinition);
	}

	function validateValue(type: ConfigValueType, raw: string, enumDefinition: string): string | null {
		const normalizedRaw = String(raw ?? '');
		if (type === CONFIG_VALUE_TYPE.DateTime && Number.isNaN(Date.parse(raw))) return 'DateTime value must be valid.';
		if (type === CONFIG_VALUE_TYPE.TimeSpan && !/^\d{2}:\d{2}:\d{2}$/.test(raw)) return 'TimeSpan must be in HH:mm:ss format.';
		if (type === CONFIG_VALUE_TYPE.Json) {
			try {
				JSON.parse(raw);
			} catch {
				return 'JSON value is invalid.';
			}
		}
		if (type === CONFIG_VALUE_TYPE.Array) {
			try {
				if (!Array.isArray(JSON.parse(raw))) return 'Array value must be a JSON array.';
			} catch {
				return 'Array value must be a valid JSON array.';
			}
		}
		if (type === CONFIG_VALUE_TYPE.Number) {
			if (!isValidNumberInput(normalizedRaw)) return 'Number value is invalid.';
		}
		if (type === CONFIG_VALUE_TYPE.Boolean) {
			if (!['true', 'false'].includes(normalizeBooleanValue(normalizedRaw))) return "Value must be 'true' or 'false'.";
		}
		if (type === CONFIG_VALUE_TYPE.Enum) {
			const values = enumDefinition
				.split(',')
				.map((x) => x.trim())
				.filter(Boolean);
			if (values.length === 0) return 'Enum values are required.';
			if (!values.includes(normalizedRaw)) return 'Enum value must be one of Enum values.';
		}
		return null;
	}

	function isValidNumberInput(value: string): boolean {
		const normalized = String(value ?? '').trim();
		return normalized !== '' && !Number.isNaN(Number(normalized));
	}

	function normalizeBooleanValue(value: string): string {
		const normalized = String(value ?? '').trim().toLowerCase();
		if (['1', 'yes', 'on'].includes(normalized)) return 'true';
		if (['0', 'no', 'off'].includes(normalized)) return 'false';
		return normalized;
	}

	function normalizeRawValueForSave(raw: string, type: ConfigValueType): string {
		const normalized = String(raw ?? '');
		if (type === CONFIG_VALUE_TYPE.DateTime) return new Date(normalized).toISOString();
		if (type === CONFIG_VALUE_TYPE.Boolean) return normalizeBooleanValue(normalized);
		return normalized;
	}

	function resetPanel(groupName = '') {
		formId = '';
		formKey = '';
		formValueType = CONFIG_VALUE_TYPE.String;
		formRawValue = '';
		formArrayItems = [''];
		formEnumDefinition = '';
		formDescription = '';
		formGroupName = normalizedGroupName(groupName);
		formGroupDescription = knownGroups.find((group) => group.name === formGroupName)?.description ?? '';
		actionError = '';
		formValueError = '';
	}

	function openCreatePanel(groupName = '') {
		if (!userCanAll) return;
		panelMode = 'create';
		resetPanel(groupName);
		showPanel = true;
	}

	function openEditPanel(entry: ConfigEntry) {
		panelMode = 'edit';
		formId = entry.id;
		formKey = entry.key;
		formValueType = normalizeValueType(entry.valueType);
		formRawValue = isBooleanValueType(entry.valueType)
			? normalizeBooleanValue(entry.rawValue ?? 'false')
			: entry.rawValue ?? '';
		formArrayItems = isArrayValueType(entry.valueType) ? parseArrayItems(entry.rawValue) : [''];
		formEnumDefinition = entry.enumDefinition ?? '';
		formDescription = entry.description ?? '';
		formGroupName = normalizedGroupName(entry.groupName);
		formGroupDescription = entry.groupDescription?.trim() ?? knownGroups.find((g) => g.name === formGroupName)?.description ?? '';
		actionError = '';
		formValueError = '';
		showPanel = true;
	}

	function suggestSuccessorLabel(label?: string | null): string {
		const normalized = label?.trim();
		if (!normalized) return '';
		const match = normalized.match(/^(.*?)(\d+)$/);
		if (!match) return `${normalized}-next`;

		const [, prefix, digits] = match;
		const nextNumber = String(Number(digits) + 1).padStart(digits.length, '0');
		return `${prefix}${nextNumber}`;
	}

	function openCreateSuccessorModal() {
		if (!userCanAll || !version) return;
		newVersionLabel = suggestSuccessorLabel(version.versionLabel);
		newVersionDescription = version.description ?? '';
		actionError = '';
		showNewVersionModal = true;
	}

	async function createSuccessorVersion() {
		if (!service || !newVersionLabel.trim()) return;
		actionError = '';
		creatingVersion = true;

		try {
			const createdVersion = await apiRequest<VersionDetail>(`/configversions/${service.id}`, {
				method: 'POST',
				body: JSON.stringify({
					versionLabel: newVersionLabel.trim(),
					description: newVersionDescription
				})
			});

			for (const entry of entries) {
				await apiRequest<ConfigEntry>(`/configentries/${service.id}/${createdVersion.id}`, {
					method: 'POST',
					body: JSON.stringify({
						key: entry.key,
						name: entry.name || entry.key,
						rawValue: entry.rawValue,
						valueType: normalizeValueType(entry.valueType),
						enumDefinition: entry.enumDefinition,
						description: entry.description,
						groupName: normalizedGroupName(entry.groupName) || null,
						groupDescription: normalizedGroupName(entry.groupName) ? entry.groupDescription : null
					})
				});
			}

			showNewVersionModal = false;
			newVersionLabel = '';
			newVersionDescription = '';
			versions = sortVersions([...versions, createdVersion]);
			pushToast('Successor version created');
			await goto(versionHref(createdVersion.id));
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to create successor version');
		} finally {
			creatingVersion = false;
		}
	}

	function markChanged(key: string) {
		changedRows = { ...changedRows, [key]: Date.now() };
		setTimeout(() => {
			const next = { ...changedRows };
			delete next[key];
			changedRows = next;
		}, 1000);
	}

	function upsertLocalEntry(entry: ConfigEntry) {
		const idx = entries.findIndex((x) => x.id === entry.id || x.key === entry.key);
		const next = { ...entry, updatedAt: new Date().toISOString() };
		if (idx >= 0) {
			entries[idx] = next;
			entries = [...entries];
		} else {
			entries = [next, ...entries];
		}
		loadCollapsedState();
		markChanged(entry.key);
	}

	function removeLocalEntry(idOrKey: string) {
		const removed = entries.find((x) => x.id === idOrKey || x.key === idOrKey);
		entries = entries.filter((x) => x.id !== idOrKey && x.key !== idOrKey);
		loadCollapsedState();
		if (removed) markChanged(removed.key);
	}

	function pushToast(message: string) {
		const id = toastSeed++;
		toasts = [...toasts, { id, message }];
		setTimeout(() => {
			toasts = toasts.filter((x) => x.id !== id);
		}, 2600);
	}

	function prettyDate(value?: string | null): string {
		if (!value) return '-';
		const parsed = new Date(value);
		return Number.isNaN(parsed.valueOf()) ? '-' : parsed.toLocaleString();
	}

	async function saveInfo() {
		if (!service) return;
		actionError = '';
		savingService = true;
		try {
			await apiRequest<ServiceDetail>(`/services/${service.id}`, {
				method: 'PUT',
				body: JSON.stringify({
					name: editName,
					description: editDescription,
					repositoryUrl: editRepositoryUrl,
					contactEmail: editContactEmail
				})
			});
			showEdit = false;
			await loadServiceDetails();
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to save service');
		} finally {
			savingService = false;
		}
	}

	async function loadServiceDetails() {
		service = await apiRequest<ServiceDetail>(`/services/${serviceId}`);
		editName = service.name;
		editDescription = service.description ?? '';
		editRepositoryUrl = service.repositoryUrl ?? '';
		editContactEmail = service.contactEmail ?? '';
	}

	async function loadAudit(pageNumber = 1) {
		auditLoading = true;
		try {
			if (pageNumber > auditPage && auditNextPageToken) {
				auditPageTokens = [...auditPageTokens.slice(0, auditPage), auditNextPageToken];
			}
			auditPage = pageNumber;
			const payload = await apiRequest<PageResponse<AuditEntry>>(`/audit/${serviceId}/search`, {
				method: 'POST',
				body: JSON.stringify({
					pageSize: auditPageSize,
					pageToken: auditPageTokens[auditPage - 1] || null
				})
			});
			audit = payload.entities;
			auditNextPageToken = payload.nextPageToken ?? '';
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to load audit log');
		} finally {
			auditLoading = false;
		}
	}

	function toggleRow(index: number) {
		const key = `${auditPage}-${index}`;
		expandedRows = { ...expandedRows, [key]: !expandedRows[key] };
	}

	function stringifyJson(value: unknown): string {
		if (value === null || value === undefined) return '-';
		try {
			return JSON.stringify(value, null, 2);
		} catch {
			return String(value);
		}
	}

	function auditType(entry: AuditEntry['entry']): string {
		const rawType = entry?.$type ?? entry?.type;
		if (typeof rawType === 'string') return rawType;
		if (typeof rawType !== 'number') return 'Unknown';

		const types: Record<number, string> = {
			0: 'ServiceCreated',
			1: 'ServiceUpdated',
			2: 'ServiceMemberAdded',
			3: 'ServiceMemberRemoved',
			4: 'ServiceDeleted',
			5: 'VersionCreated',
			6: 'VersionUpdated',
			7: 'EntryCreated',
			8: 'EntryUpdated',
			9: 'EntryDeleted',
			10: 'EntrySet'
		};
		return types[rawType] ?? 'Unknown';
	}

	function auditActionLabel(type: string): string {
		const labels: Record<string, string> = {
			ServiceCreated: 'Service created',
			ServiceUpdated: 'Service updated',
			ServiceMemberAdded: 'Member added',
			ServiceMemberRemoved: 'Member removed',
			ServiceDeleted: 'Service deleted',
			VersionCreated: 'Version created',
			VersionUpdated: 'Version updated',
			EntryCreated: 'Config entry created',
			EntryUpdated: 'Config entry updated',
			EntryDeleted: 'Config entry deleted',
			EntrySet: 'Config value changed'
		};
		return labels[type] ?? type.replace(/([a-z])([A-Z])/g, '$1 $2');
	}

	function auditBadge(type: string): AuditSummary['badge'] {
		if (type.endsWith('Created') || type === 'ServiceMemberAdded') return 'success';
		if (type.endsWith('Updated') || type === 'EntrySet') return 'warning';
		if (type.endsWith('Deleted') || type === 'ServiceMemberRemoved') return 'danger';
		return 'accent';
	}

	function humanLabel(key: string): string {
		const labels: Record<string, string> = {
			id: 'ID',
			name: 'Name',
			description: 'Description',
			repositoryUrl: 'Repository URL',
			contactEmail: 'Contact email',
			userId: 'User ID',
			versionLabel: 'Version',
			rawValue: 'Value',
			enumDefinition: 'Enum values',
			groupName: 'Group',
			groupDescription: 'Group description',
			value: 'Value'
		};
		return labels[key] ?? key.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, (value) => value.toUpperCase());
	}

	function formatAuditValue(value: unknown): string {
		if (value === null || value === undefined || value === '') return 'Not set';
		if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') return String(value);
		return stringifyJson(value);
	}

	function auditDetails(entry: AuditEntry['entry']): AuditDetail[] {
		const hidden = new Set(['$type', 'type']);
		return Object.entries(entry ?? {})
			.filter(([key]) => !hidden.has(key))
			.map(([key, value]) => ({ label: humanLabel(key), value: formatAuditValue(value) }));
	}

	function auditSubject(type: string, entry: AuditEntry['entry']): string {
		if (type.startsWith('Service')) return formatAuditValue(entry?.name);
		if (type.startsWith('Version')) return formatAuditValue(entry?.versionLabel ?? entry?.id);
		if (type.startsWith('Entry')) return `Entry ${formatAuditValue(entry?.id)}`;
		return 'Audit event';
	}

	function auditSummary(row: AuditEntry): AuditSummary {
		const type = auditType(row.entry);
		const subject = auditSubject(type, row.entry);
		const details = auditDetails(row.entry);
		return {
			type,
			title: auditActionLabel(type),
			subtitle: subject === 'Not set' ? 'No target details' : subject,
			badge: auditBadge(type),
			details,
			unknown: type === 'Unknown'
		};
	}

	function applyEntries(nextEntries: ConfigEntry[]) {
		entries = nextEntries.map((x) => ({ ...x, updatedAt: null }));
		pendingValues = {};
		showOnlyChanged = false;
		cancelInlineEdit();
		loadCollapsedState();
	}

	async function loadInitialPage() {
		loading = true;
		error = '';
		entriesError = '';
		try {
			const [versionData, versionList] = await Promise.all([
				apiRequest<VersionDetail>(`/configversions/${serviceId}/${versionId}`),
				apiRequest<VersionDetail[]>(`/configversions/${serviceId}`)
			]);
			const entryData = await apiRequest<ConfigEntry[]>(`/configentries/${serviceId}/${versionId}`);
			await loadServiceDetails();
			version = versionData;
			versions = sortVersions(versionList);
			applyEntries(entryData);
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to load entries');
		} finally {
			loading = false;
		}
	}

	async function loadVersionEntries(nextVersionId = versionId) {
		const requestKey = `${serviceId}:${nextVersionId}`;
		entriesLoading = true;
		entriesError = '';
		actionError = '';
		pendingValues = {};
		showOnlyChanged = false;
		cancelInlineEdit();
		changedRows = {};

		const localVersion = versions.find((item) => item.id === nextVersionId);
		if (localVersion) version = localVersion;

		try {
			const [versionData, entryData] = await Promise.all([
				localVersion ? Promise.resolve(localVersion) : apiRequest<VersionDetail>(`/configversions/${serviceId}/${nextVersionId}`),
				apiRequest<ConfigEntry[]>(`/configentries/${serviceId}/${nextVersionId}`)
			]);
			if (requestKey !== activePageKey) return;
			version = versionData;
			applyEntries(entryData);
		} catch (e) {
			if (requestKey !== activePageKey) return;
			entriesError = getApiErrorMessage(e, 'Failed to load entries');
		} finally {
			if (requestKey === activePageKey) entriesLoading = false;
		}
	}

	async function savePanel() {
		actionError = '';
		formValueError = '';
		const rawForSave = formValueType === CONFIG_VALUE_TYPE.Array ? serializeArrayItems(formArrayItems) : formRawValue;
		const validation = validateValue(formValueType, rawForSave, formEnumDefinition);
		if (validation) {
			formValueError = validation;
			return;
		}

		const normalizedGroup = normalizedGroupName(formGroupName);
		const normalizedDescription = normalizedGroup ? formGroupDescription.trim() : '';

		saving = true;
		try {
			if (panelMode === 'create') {
				const rawValue = normalizeRawValueForSave(rawForSave, formValueType);
				if (browser && formValueType === CONFIG_VALUE_TYPE.Number) {
					console.debug('Saving number config entry', { panelMode, key: formKey, rawValue });
				}
				const created = await apiRequest<ConfigEntry>(`/configentries/${serviceId}/${versionId}`, {
					method: 'POST',
					body: JSON.stringify({
						key: formKey,
						name: formKey,
						rawValue,
						valueType: formValueType,
						enumDefinition: formValueType === CONFIG_VALUE_TYPE.Enum ? formEnumDefinition.trim() : null,
						description: formDescription,
						groupName: normalizedGroup || null,
						groupDescription: normalizedDescription || null
					})
				});
				upsertLocalEntry(created);
			} else {
				const rawValue = normalizeRawValueForSave(rawForSave, formValueType);
				if (browser && formValueType === CONFIG_VALUE_TYPE.Number) {
					console.debug('Saving number config entry', { panelMode, key: formKey, rawValue });
				}
				const updateRequest = userCanAll
					? {
							rawValue,
							enumDefinition: formValueType === CONFIG_VALUE_TYPE.Enum ? formEnumDefinition.trim() : null,
							description: formDescription,
							groupName: normalizedGroup || '',
							groupDescription: normalizedDescription || '',
							generation: entries.find((entry) => entry.id === formId)?.generation ?? 0
						}
					: {
							rawValue,
							generation: entries.find((entry) => entry.id === formId)?.generation ?? 0
						};
				const updated = await apiRequest<ConfigEntry>(`/configentries/${serviceId}/${versionId}/${formId}`, {
					method: 'PUT',
					body: JSON.stringify(updateRequest)
				});
				upsertLocalEntry(updated);
			}
			showPanel = false;
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to save entry');
		} finally {
			saving = false;
		}
	}

	function requestDelete(entry: ConfigEntry) {
		if (!userCanAll) return;
		pendingDelete = entry;
		confirmDeleteOpen = true;
	}

	async function deleteEntry() {
		if (!pendingDelete) return;
		deleting = true;
		actionError = '';
		try {
			await apiRequest<unknown>(`/configentries/${serviceId}/${versionId}/${pendingDelete.id}`, {
				method: 'DELETE'
			});
			removeLocalEntry(pendingDelete.id || pendingDelete.key);
			confirmDeleteOpen = false;
			pendingDelete = null;
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to delete entry');
		} finally {
			deleting = false;
		}
	}

	function startInlineEdit(entry: ConfigEntry) {
		if (!canInlineEdit(entry)) return;
		inlineId = entry.id;
		inlineValue = isBooleanValueType(entry.valueType)
			? normalizeBooleanValue(getEntryValue(entry) || 'false')
			: getEntryValue(entry);
		inlineArrayItems = isArrayValueType(entry.valueType) ? parseArrayItems(getEntryValue(entry)) : [];
		inlineValueError = pendingEntryErrors[entry.id] ?? '';
	}

	function cancelInlineEdit() {
		inlineId = '';
		inlineValue = '';
		inlineArrayItems = [];
		inlineValueError = '';
	}

	function saveInline(entry: ConfigEntry) {
		if (!inlineId) return;
		const type = normalizeValueType(entry.valueType);
		const rawForSave = type === CONFIG_VALUE_TYPE.Array ? serializeArrayItems(inlineArrayItems) : inlineValue;
		const validation = validateValue(type, rawForSave, entry.enumDefinition ?? '');
		if (validation) {
			actionError = '';
			inlineValueError = validation;
			pendingEntryErrors = { ...pendingEntryErrors, [entry.id]: validation };
			return;
		}

		actionError = '';
		inlineValueError = '';
		setPendingValue(entry, String(normalizeRawValueForSave(rawForSave, type)));
		cancelInlineEdit();
	}

	async function savePendingChanges() {
		if (!canBatchSave) return;

		actionError = '';
		pendingEntryErrors = {};
		for (const entry of pendingEntries) {
			const type = normalizeValueType(entry.valueType);
			const validation = validateValue(type, getEntryValue(entry), entry.enumDefinition ?? '');
			if (validation) {
				pendingEntryErrors = { [entry.id]: validation };
				if (inlineId === entry.id) inlineValueError = validation;
				return;
			}
		}

		batchSaving = true;
		try {
			const updated = await apiRequest<ConfigEntry[]>(`/configentries/${serviceId}/${versionId}/set`, {
				method: 'PUT',
				body: JSON.stringify(
					pendingEntries.map((entry) => ({
						id: entry.id,
						rawValue: getEntryValue(entry),
						generation: entry.generation
					}))
				)
			});

			for (const entry of updated) {
				upsertLocalEntry(entry);
			}
			pendingValues = {};
			pendingEntryErrors = {};
			showOnlyChanged = false;
			cancelInlineEdit();
			pushToast('Config changes saved');
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to save changes');
		} finally {
			batchSaving = false;
		}
	}

	async function initRealtime() {
		if (!browser) return;

		connectionStatus = 'connecting';
		const signalrUrl = env.PUBLIC_SIGNALR_URL?.trim() || buildBackendUrl('/hubs/config');
		connection = new HubConnectionBuilder()
			.withUrl(signalrUrl, { withCredentials: true })
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();

		connection.onreconnecting(() => {
			connectionStatus = 'reconnecting';
		});

		connection.onreconnected(() => {
			connectionStatus = 'live';
		});

		connection.onclose(() => {
			connectionStatus = 'offline';
		});

		connection.on('ConfigChanged', (payload: any) => {
			const requests = (payload?.requests ?? payload?.Requests ?? []) as { id?: string; Id?: string; value?: string | null; Value?: string | null }[];
			if (requests.length === 0) {
				void loadVersionEntries(connectionVersionId || versionId);
				return;
			}

			let changed = 0;
			for (const request of requests) {
				const id = request.id ?? request.Id;
				if (!id) continue;

				const idx = entries.findIndex((entry) => entry.id === id);
				if (idx < 0) {
					void loadVersionEntries(connectionVersionId || versionId);
					continue;
				}

				const current = entries[idx];
				const next = {
					...current,
					rawValue: request.value ?? request.Value ?? null,
					generation: current.generation + 1,
					updatedAt: new Date().toISOString()
				};
				entries[idx] = next;
				delete pendingValues[next.id];
				markChanged(next.key);
				changed++;
			}

			if (changed > 0) {
				entries = [...entries];
				pendingValues = { ...pendingValues };
				pushToast(`${changed} config ${changed === 1 ? 'entry' : 'entries'} updated`);
			}
		});

		try {
			await connection.start();
			await connection.invoke('JoinVersionGroup', serviceId, versionId);
			connectionStatus = 'live';
			connectionServiceId = serviceId;
			connectionVersionId = versionId;
		} catch (e) {
			connectionStatus = 'offline';
			throw e;
		}
	}

	async function teardownRealtime() {
		connectionStatus = 'offline';
		if (!connection) return;
		try {
			await connection.invoke('LeaveVersionGroup', connectionServiceId, connectionVersionId);
		} catch {
			// no-op
		}
		await connection.stop();
		connection = null;
		connectionServiceId = '';
		connectionVersionId = '';
	}

	async function loadCurrentVersionPage() {
		const pageKey = `${serviceId}:${versionId}`;
		activePageKey = pageKey;
		await teardownRealtime();
		if (!service || versions.length === 0) {
			await loadInitialPage();
		} else {
			await loadVersionEntries(versionId);
		}
		if (activePageKey === pageKey && !error && !entriesError) await initRealtime();
	}

	onMount(() => {
		mounted = true;
		void loadCurrentVersionPage();
		return () => {
			mounted = false;
			void teardownRealtime();
		};
	});

	$effect(() => {
		const pageKey = `${serviceId}:${versionId}`;
		if (!mounted || pageKey === activePageKey) return;
		void loadCurrentVersionPage();
	});

	$effect(() => {
		if (formValueType === CONFIG_VALUE_TYPE.Boolean && formRawValue !== 'true' && formRawValue !== 'false') {
			formRawValue = 'false';
		}
		if (formValueType === CONFIG_VALUE_TYPE.Array && formArrayItems.length === 0) {
			formArrayItems = [''];
		}
		formValueError = '';
	});
</script>

<section class={`space-y-6 ${pendingCount > 0 ? 'pb-28' : ''}`}>
	<div class="pt-2">
		{#if mainTab === 'info'}
			<Card>
				{#snippet header()}
					<div class="flex items-center justify-between gap-3">
						<div>
							<p class="section-label">Overview</p>
							<h2 class="mt-2 text-[20px] font-semibold">Service information</h2>
						</div>
						{#if userCanAll}
							<Button variant="secondary" onclick={() => (showEdit = !showEdit)}>{showEdit ? 'Cancel' : 'Edit'}</Button>
						{/if}
					</div>
				{/snippet}

				{#if showEdit && userCanAll}
					<div class="max-w-[640px] space-y-5">
						<Input label="Name" bind:value={editName} />
						<Textarea label="Description" bind:value={editDescription} />
						<Input label="Repository URL" bind:value={editRepositoryUrl} />
						<Input label="Contact Email" bind:value={editContactEmail} />
						<div class="flex justify-end">
							<Button loading={savingService} onclick={saveInfo}>Save changes</Button>
						</div>
					</div>
				{:else if service}
					<div class="grid gap-5 md:grid-cols-2">
						<div class="space-y-3">
							<p class="text-[13px] text-[var(--text-secondary)]">Name</p>
							<p class="text-[16px]">{service.name}</p>
						</div>
						<div class="space-y-3">
							<p class="text-[13px] text-[var(--text-secondary)]">Repository</p>
							{#if service.repositoryUrl}
								<a href={service.repositoryUrl} target="_blank" rel="noreferrer" class="text-[14px] text-[var(--accent-text)] hover:underline">{service.repositoryUrl}</a>
							{:else}
								<p class="text-[14px] text-[var(--text-secondary)]">Not linked</p>
							{/if}
						</div>
						<div class="space-y-3 md:col-span-2">
							<p class="text-[13px] text-[var(--text-secondary)]">Description</p>
							<p class="text-[14px] text-[var(--text-primary)]">{service.description ?? 'No description available.'}</p>
						</div>
						<div class="space-y-3">
							<p class="text-[13px] text-[var(--text-secondary)]">Contact Email</p>
							<p class="text-[14px] text-[var(--text-primary)]">{service.contactEmail ?? '-'}</p>
						</div>
						<div class="space-y-3">
							<p class="text-[13px] text-[var(--text-secondary)]">Created</p>
							<p class="text-[14px] text-[var(--text-primary)]">{prettyDate(service.createdAt)}</p>
						</div>
						<div class="space-y-3">
							<p class="text-[13px] text-[var(--text-secondary)]">Updated</p>
							<p class="text-[14px] text-[var(--text-primary)]">{prettyDate(service.updatedAt)}</p>
						</div>
					</div>
				{/if}
			</Card>
		{:else if mainTab === 'versions'}
			<div class="mb-4 flex flex-wrap items-start justify-between gap-3">
				<div>
					<p class="section-label">Config</p>
					<div class="mt-2 flex flex-wrap items-center gap-3">
						<h2 class="text-[20px] font-semibold">Versions</h2>
						{#if pendingCount > 0}
							<span class="metric-pill">{pendingCount} unsaved</span>
						{/if}
					</div>
					<div class="mt-1 flex flex-wrap items-center gap-2">
						<p class="text-[13px] text-[var(--text-secondary)]">
							{version?.versionLabel ?? 'Version'}{version?.description ? ` · ${version.description}` : ''}
						</p>
						{#if userCanAll && versions.length > 0}
							<Button variant="secondary" size="sm" onclick={openCreateSuccessorModal}>Create successor</Button>
						{/if}
					</div>
				</div>
			</div>

			{#if versions.length > 0}
				<div class="version-carousel" aria-label="Versions">
					<div class="version-tabs">
						{#each versions as item}
							<a class={`version-tab ${item.id === versionId ? 'active' : ''}`} href={versionHref(item.id)} aria-current={item.id === versionId ? 'page' : undefined}>
								<span class="version-tab-label">{item.versionLabel}</span>
								<span class="version-tab-date">{new Date(item.createdAt).toLocaleDateString()}</span>
							</a>
						{/each}
					</div>
				</div>
			{/if}

			{#if pendingCount > 0}
				<div class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius-md)] border border-[var(--border)] bg-[var(--bg-elevated)] px-4 py-3">
					<label class="inline-flex items-center gap-2 text-[13px] text-[var(--text-primary)]">
						<input class="h-4 w-4 accent-[var(--accent)]" type="checkbox" bind:checked={showOnlyChanged} />
						<span>Show changes</span>
					</label>
					<div class="text-[13px] text-[var(--text-secondary)]">
						{pendingCount} unsaved {pendingCount === 1 ? 'change' : 'changes'}
					</div>
				</div>
			{/if}

	{#if loading || entriesLoading}
		<div class="space-y-4">
			<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
			<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
		</div>
	{:else if error}
		<ErrorCallout message={error} />
	{:else if entriesError}
		<ErrorCallout message={entriesError} />
	{:else if groupedEntries.length === 0}
		<EmptyState title={showOnlyChanged ? 'No changed config entries' : 'No config entries yet'} description={showOnlyChanged ? 'Change values first, then enable this filter to review them before saving.' : 'Create the first key for this version or let the SDK register missing keys automatically.'}>
			{#snippet icon()}
				<svg viewBox="0 0 48 48" class="h-12 w-12" fill="none" aria-hidden="true">
					<rect x="10" y="8" width="28" height="32" rx="6" stroke="currentColor" stroke-width="2" />
					<path d="M17 18h14M17 24h14M17 30h8" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
				</svg>
			{/snippet}
			{#snippet action()}
				{#if userCanAll}
					<Button onclick={() => openCreatePanel()}>Add entry</Button>
				{/if}
			{/snippet}
		</EmptyState>
	{:else}
		<div class="space-y-4">
			{#each groupedEntries as group (group.key)}
				<Card>
					{#snippet header()}
						<div class="flex items-center justify-between gap-3">
							<div>
								<h2 class="text-[20px] font-semibold">{group.title}</h2>
								{#if group.description}
									<p class="mt-1 text-[13px] text-[var(--text-secondary)]">{group.description}</p>
								{:else if group.isUngrouped}
									<p class="mt-1 text-[13px] text-[var(--text-secondary)]">Entries without a group heading.</p>
								{/if}
							</div>
							<div class="flex items-center gap-3">
								{#if !group.isUngrouped}
									<Button variant="ghost" size="sm" onclick={() => toggleGroup(group.title)}>{group.collapsed ? 'Expand' : 'Collapse'}</Button>
								{/if}
							</div>
						</div>
					{/snippet}

					{#if group.isUngrouped || !group.collapsed}
						<div class="space-y-4">
							{#if userCanAll}
								<div class="flex justify-end">
									<Button variant="secondary" size="sm" onclick={() => openCreatePanel(group.isUngrouped ? '' : group.title)}>Add entry</Button>
								</div>
							{/if}
							<div class="overflow-x-auto">
								<table class="min-w-full text-left text-[13px]">
									<thead>
										<tr class="h-10 border-b border-[var(--border)] bg-[var(--bg-subtle)] text-[12px] uppercase tracking-[0.08em] text-[var(--text-tertiary)]">
											<th class="px-3">Key</th>
											<th class="px-3">Type</th>
											<th class="px-3">Value</th>
											<th class="px-3">Description</th>
											<th class="px-3 text-right">Actions</th>
										</tr>
									</thead>
									<tbody>
										{#each group.entries as entry}
											<tr class={`h-11 border-b border-[var(--border)] ${changedRows[entry.key] ? 'row-flash' : ''} ${isEntryChanged(entry) ? 'bg-[var(--accent-subtle)]/35' : ''}`}>
												<td class="px-3 py-3">
													<div class="flex items-center gap-2">
														<span class="mono text-[13px] text-[var(--text-primary)]">{entry.key}</span>
														{#if isEntryChanged(entry)}
															<Badge variant="accent">Changed</Badge>
														{/if}
													</div>
												</td>
												<td class="px-3 py-3"><Badge variant={valueTypeBadge(entry.valueType)}>{valueTypeLabel(entry.valueType)}</Badge></td>
												<td class="px-3 py-3">
													{#if inlineId === entry.id}
														{#if isBooleanValueType(entry.valueType)}
															<label class="toggle-inline">
																<input
																	type="checkbox"
																	checked={inlineValue === 'true'}
																	onchange={(e) => {
																		inlineValue = e.currentTarget.checked ? 'true' : 'false';
																		saveInline(entry);
																	}}
																/>
																<span class="toggle-track">
																	<span class="toggle-thumb"></span>
																</span>
																<span class="toggle-label">{inlineValue === 'true' ? 'true' : 'false'}</span>
															</label>
														{:else if isEnumValueType(entry.valueType) && entryEnumValues(entry).length > 0}
															<div class="flex items-center gap-2">
																<select
																	class="h-9 w-full rounded-[8px] border border-[var(--accent)] bg-[var(--bg-surface)] px-3 text-[13px] text-[var(--text-primary)] outline-none"
																	bind:value={inlineValue}
																>
																	{#each entryEnumValues(entry) as value}
																		<option value={value}>{value}</option>
																	{/each}
																</select>
																<Button variant="secondary" size="sm" onclick={() => saveInline(entry)}>
																	✓
																</Button>
																<Button variant="ghost" size="sm" onclick={cancelInlineEdit}>✕</Button>
															</div>
														{:else if normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Json}
															<div class="space-y-1.5">
																<div class="flex items-start gap-2">
																	<textarea
																		class={`min-h-24 w-full rounded-[8px] border bg-[var(--bg-surface)] px-3 py-2 mono text-[13px] outline-none ${inlineValueError ? 'border-[var(--danger)]' : 'border-[var(--accent)]'}`}
																		bind:value={inlineValue}
																		oninput={() => {
																			inlineValueError = '';
																		}}
																		onkeydown={(e) => {
																			if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') saveInline(entry);
																			if (e.key === 'Escape') cancelInlineEdit();
																		}}
																	></textarea>
																	<div class="flex flex-col gap-2">
																		<Button variant="secondary" size="sm" onclick={() => saveInline(entry)}>✓</Button>
																		<Button variant="ghost" size="sm" onclick={cancelInlineEdit}>✕</Button>
																	</div>
																</div>
																{#if inlineValueError}
																	<p class="field-error">{inlineValueError}</p>
																{/if}
															</div>
														{:else if isArrayValueType(entry.valueType)}
															<div class="array-editor array-editor-inline">
																<div class="space-y-2">
																	{#each inlineArrayItems as item, index}
																		<div class="array-row">
																			<input
																				class="array-input mono"
																				type="text"
																				value={item}
																				placeholder={`Item ${index + 1}`}
																				oninput={(e) => updateInlineArrayItem(index, e.currentTarget.value)}
																				onkeydown={(e) => {
																					if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') saveInline(entry);
																					if (e.key === 'Escape') cancelInlineEdit();
																				}}
																			/>
																			<Button variant="ghost" size="sm" className="array-icon-button" onclick={() => removeInlineArrayItem(index)}>−</Button>
																		</div>
																	{/each}
																	{#if inlineArrayItems.length === 0}
																		<div class="array-empty">Empty array</div>
																	{/if}
																</div>
																<div class="flex flex-wrap items-center gap-2">
																	<Button variant="secondary" size="sm" onclick={addInlineArrayItem}>Add row</Button>
																	<Button variant="secondary" size="sm" onclick={() => saveInline(entry)}>✓</Button>
																	<Button variant="ghost" size="sm" onclick={cancelInlineEdit}>✕</Button>
																</div>
																{#if inlineValueError}
																	<p class="field-error">{inlineValueError}</p>
																{/if}
															</div>
														{:else}
															<div class="space-y-1.5">
																<div class="flex items-center gap-2">
																	<input
																		class={`h-9 w-full rounded-[8px] border bg-[var(--bg-surface)] px-3 mono text-[13px] outline-none ${inlineValueError ? 'border-[var(--danger)]' : 'border-[var(--accent)]'}`}
																		type="text"
																		inputmode={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? 'decimal' : undefined}
																		pattern={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? '[0-9]*\\.?[0-9]*' : undefined}
																		placeholder={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? 'e.g. 42 or 3.14' : undefined}
																		bind:value={inlineValue}
																		oninput={() => {
																			inlineValueError = '';
																		}}
																		onkeydown={(e) => {
																			if (e.key === 'Enter') saveInline(entry);
																			if (e.key === 'Escape') cancelInlineEdit();
																		}}
																	/>
																	<Button variant="secondary" size="sm" onclick={() => saveInline(entry)}>
																		✓
																	</Button>
																	<Button variant="ghost" size="sm" onclick={cancelInlineEdit}>✕</Button>
																</div>
																{#if inlineValueError}
																	<p class="field-error">{inlineValueError}</p>
																{/if}
															</div>
														{/if}
													{:else}
														{#if isEnumValueType(entry.valueType) && entryEnumValues(entry).length > 0}
															<button
																class="disabled:cursor-default"
																type="button"
																disabled={!canInlineEdit(entry)}
																onclick={() => startInlineEdit(entry)}
															>
																<Badge variant="warning">{getEntryValue(entry) || '-'}</Badge>
															</button>
														{:else if isBooleanValueType(entry.valueType)}
															<button
																class="disabled:cursor-default"
																type="button"
																disabled={!canInlineEdit(entry)}
																onclick={() => setPendingValue(entry, getEntryValue(entry) === 'true' ? 'false' : 'true')}
															>
																<span class={`bool-pill ${getEntryValue(entry) === 'true' ? 'bool-on' : 'bool-off'}`}>
																	<span class="bool-dot"></span>
																	{getEntryValue(entry) === 'true' ? 'true' : 'false'}
																</span>
															</button>
														{:else if isArrayValueType(entry.valueType)}
															<button
																class="block max-w-[420px] text-left disabled:cursor-default"
																type="button"
																disabled={!canInlineEdit(entry)}
																onclick={() => startInlineEdit(entry)}
															>
																{#if arrayItemsForDisplay(getEntryValue(entry)).length > 0}
																	<span class="array-preview">
																		{#each arrayItemsForDisplay(getEntryValue(entry)).slice(0, 3) as item}
																			<span class="array-chip mono">{item}</span>
																		{/each}
																		{#if arrayItemsForDisplay(getEntryValue(entry)).length > 3}
																			<span class="array-more">+{arrayItemsForDisplay(getEntryValue(entry)).length - 3}</span>
																		{/if}
																	</span>
																{:else}
																	<span class="mono text-[13px] text-[var(--text-tertiary)]">[]</span>
																{/if}
															</button>
														{:else}
															<button
																class="mono text-[13px] text-[var(--text-primary)] hover:text-[var(--accent-text)] disabled:cursor-default disabled:text-[var(--text-primary)]"
																type="button"
																disabled={!canInlineEdit(entry)}
																onclick={() => startInlineEdit(entry)}
															>
																<span class="block max-w-[340px] truncate">{getEntryValue(entry) || '-'}</span>
															</button>
														{/if}
														{#if pendingEntryErrors[entry.id]}
															<p class="field-error mt-1">{pendingEntryErrors[entry.id]}</p>
														{/if}
													{/if}
												</td>
												<td class="px-3 py-3 text-[var(--text-secondary)]">{entry.description ?? ''}</td>
												<td class="px-3 py-3">
													<div class="flex justify-end gap-2">
														{#if canEditExistingEntries}
															<Button variant="ghost" size="sm" onclick={() => openEditPanel(entry)}>Edit</Button>
														{/if}
														{#if userCanAll}
															<Button variant="ghost" size="sm" onclick={() => requestDelete(entry)}>Delete</Button>
														{/if}
													</div>
												</td>
											</tr>
										{/each}
									</tbody>
								</table>
							</div>
						</div>
					{/if}
				</Card>
			{/each}
			</div>
		{/if}
			{:else}
				<Card>
				{#snippet header()}
					<div>
						<p class="section-label">History</p>
						<h2 class="mt-2 text-[20px] font-semibold">Audit log</h2>
					</div>
				{/snippet}

				{#if auditLoading}
					<div class="space-y-4">
						<div class="h-24 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
						<div class="h-24 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
					</div>
				{:else if audit.length === 0}
					<EmptyState title="No audit events" description="Changes to services, versions, and entries will appear here." />
				{:else}
					<div class="timeline">
						{#each audit as row, i}
							{@const rowKey = `${auditPage}-${i}`}
							{@const summary = auditSummary(row)}
							<div class="timeline-item">
								<button class="w-full rounded-[12px] border border-[var(--border)] bg-[var(--bg-elevated)] p-4 text-left" type="button" onclick={() => toggleRow(i)}>
									<div class="flex flex-wrap items-start justify-between gap-3">
										<div class="min-w-0 space-y-2">
											<Badge variant={summary.badge}>{summary.type}</Badge>
											<div>
												<p class="text-[15px] font-semibold text-[var(--text-primary)]">{summary.title}</p>
												<p class="mt-1 break-words text-[13px] text-[var(--text-secondary)]">{summary.subtitle}</p>
											</div>
										</div>
										<div class="text-right">
											<p class="text-[12px] text-[var(--text-tertiary)]">{prettyDate(row.createdAt)}</p>
											<p class="mt-1 text-[12px] text-[var(--text-secondary)]">{row.userId ? `User ${row.userId}` : 'System action'}</p>
										</div>
									</div>
								</button>
								{#if expandedRows[rowKey]}
									<div class="mt-3 rounded-[12px] border border-[var(--border)] bg-[var(--bg-elevated)] p-4">
										<div class="grid gap-3 md:grid-cols-2">
											<div>
												<p class="text-[12px] text-[var(--text-secondary)]">Changed by</p>
												<p class="mt-1 break-words text-[13px] text-[var(--text-primary)]">{row.userId ?? 'System'}</p>
											</div>
											<div>
												<p class="text-[12px] text-[var(--text-secondary)]">Created</p>
												<p class="mt-1 text-[13px] text-[var(--text-primary)]">{prettyDate(row.createdAt)}</p>
											</div>
										</div>

										{#if summary.details.length > 0}
											<div class="mt-4 grid gap-3 md:grid-cols-2">
												{#each summary.details as detail}
													<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] p-3">
														<p class="text-[12px] text-[var(--text-secondary)]">{detail.label}</p>
														<p class="mt-1 whitespace-pre-wrap break-words font-mono text-[12px] text-[var(--text-primary)]">{detail.value}</p>
													</div>
												{/each}
											</div>
										{:else}
											<p class="mt-4 text-[13px] text-[var(--text-secondary)]">No additional details.</p>
										{/if}

										{#if summary.unknown}
											<div class="mt-4">
												<p class="mb-2 text-[12px] text-[var(--text-secondary)]">Raw payload</p>
												<pre class="code-block">{stringifyJson(row.entry)}</pre>
											</div>
										{/if}
									</div>
								{/if}
							</div>
						{/each}
					</div>

					<div class="mt-4 flex items-center justify-between gap-4">
						<p class="text-[12px] text-[var(--text-secondary)]">Page {auditPage}</p>
						<div class="flex gap-3">
							<Button variant="secondary" size="sm" disabled={auditPage <= 1} onclick={() => loadAudit(auditPage - 1)}>Previous</Button>
							<Button variant="secondary" size="sm" disabled={!auditNextPageToken} onclick={() => loadAudit(auditPage + 1)}>Next</Button>
						</div>
					</div>
				{/if}
			</Card>
			{/if}
		</div>

		{#if actionError}
			<ErrorCallout message={actionError} />
		{/if}
	</section>

{#if mainTab === 'versions' && pendingCount > 0}
	<div class="fixed bottom-4 left-1/2 z-40 w-[min(760px,calc(100vw-32px))] -translate-x-1/2 rounded-[var(--radius-md)] border border-[var(--border-strong)] bg-[var(--bg-surface)] px-4 py-3 shadow-[var(--shadow-md)]">
		<div class="flex flex-wrap items-center justify-between gap-3">
			<div class="min-w-0">
				<div class="text-[14px] font-medium text-[var(--text-primary)]">{pendingCount} unsaved {pendingCount === 1 ? 'change' : 'changes'}</div>
				<div class="mt-1 text-[12px] text-[var(--text-secondary)]">Review only changed config entries before saving.</div>
			</div>
			<div class="flex flex-wrap items-center gap-3">
				<label class="inline-flex items-center gap-2 text-[13px] text-[var(--text-primary)]">
					<input class="h-4 w-4 accent-[var(--accent)]" type="checkbox" bind:checked={showOnlyChanged} />
					<span>Show changes</span>
				</label>
				<Button variant="ghost" onclick={discardPendingChanges} disabled={batchSaving}>Discard</Button>
				<Button loading={batchSaving} onclick={savePendingChanges} disabled={!canBatchSave}>Save</Button>
			</div>
		</div>
	</div>
{/if}

{#if userCanAll}
	<Modal title="Create successor" open={showNewVersionModal} onclose={() => (showNewVersionModal = false)}>
		<div class="space-y-5">
			<Input label="Version label" placeholder="e.g. v1.2.0" bind:value={newVersionLabel} />
			<Textarea label="Description" bind:value={newVersionDescription} />
			<div class="rounded-[10px] border border-[var(--border)] bg-[var(--bg-subtle)] px-4 py-3 text-[13px] text-[var(--text-secondary)]">
				Entries from {version?.versionLabel ?? 'the current version'} will be copied into the new version.
			</div>
		</div>

		{#snippet footerContent()}
			<div class="flex justify-end gap-3">
				<Button variant="ghost" onclick={() => (showNewVersionModal = false)}>Cancel</Button>
				<Button loading={creatingVersion} disabled={!newVersionLabel.trim()} onclick={createSuccessorVersion}>Create version</Button>
			</div>
		{/snippet}
	</Modal>
{/if}

<Modal title={panelMode === 'create' ? 'Add entry' : 'Edit entry'} open={showPanel} onclose={() => (showPanel = false)}>
	<div class="space-y-5">
		{#if isMemberEditingExistingEntry}
			<div class="rounded-[10px] border border-[var(--border)] bg-[var(--bg-subtle)] px-4 py-3 text-[13px] text-[var(--text-secondary)]">
				As a member you can only change the value of existing entries.
			</div>
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Group</span>
				<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 text-[14px] text-[var(--text-primary)]">{normalizedGroupName(formGroupName) || 'Ungrouped'}</div>
			</div>
			{#if normalizedGroupName(formGroupName)}
				<div class="space-y-1.5">
					<span class="block text-[13px] text-[var(--text-secondary)]">Group description</span>
					<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 text-[14px] text-[var(--text-primary)]">{formGroupDescription || '-'}</div>
				</div>
			{/if}
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Key</span>
				<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 mono text-[14px] text-[var(--text-primary)]">{formKey}</div>
			</div>
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Value type</span>
				<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 text-[14px] text-[var(--text-primary)]">{valueTypeLabel(formValueType)}</div>
			</div>
			{#if isEnumValueType(formValueType)}
				<div class="space-y-1.5">
					<span class="block text-[13px] text-[var(--text-secondary)]">Allowed values</span>
					<div class="rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 text-[14px] text-[var(--text-primary)]">{formEnumDefinition || '-'}</div>
				</div>
			{/if}
		{:else}
			<Input id="entry-group" label="Group" list="existing-groups" bind:value={formGroupName} oninput={syncGroupDescription} placeholder="Optional group name" />
			<datalist id="existing-groups">
				{#each knownGroups as group}
					<option value={group.name}></option>
				{/each}
			</datalist>

			{#if normalizedGroupName(formGroupName)}
				<Input
					id="entry-group-description"
					label="Group description"
					bind:value={formGroupDescription}
					disabled={isKnownGroupSelection}
					className={isKnownGroupSelection ? 'opacity-70' : ''}
					placeholder={isKnownGroupSelection ? 'Using existing group description' : 'Optional description'}
				/>
			{/if}

			<Input id="entry-key" label="Key" bind:value={formKey} disabled={panelMode === 'edit'} />
			<Select id="entry-type" label="Value type" bind:value={formValueType} disabled={panelMode === 'edit'}>
				{#each valueTypeOptions as option}
					<option value={option.value}>{option.label}</option>
				{/each}
			</Select>
			{#if isEnumValueType(formValueType)}
				<Input label="Allowed values" placeholder="Disabled, Internal, Beta, Everyone" bind:value={formEnumDefinition} />
			{/if}
		{/if}

		{#if formValueType === CONFIG_VALUE_TYPE.String}
			<Input
				label="Value"
				bind:value={formRawValue}
				error={formValueError}
				className="mono"
				oninput={() => {
					formValueError = '';
				}}
			/>
		{:else if formValueType === CONFIG_VALUE_TYPE.Number}
			<label class="block space-y-1.5 mono">
				<span class="block text-[13px] text-[var(--text-secondary)]">Value</span>
				<span
					class={`flex h-9 items-center gap-2 rounded-[8px] border bg-[var(--bg-surface)] px-3 transition-all duration-150 focus-within:border-[var(--accent)] ${
						formValueError ? 'border-[var(--danger)]' : 'border-[var(--border)]'
					}`}
				>
					<input
						class="w-full border-0 bg-transparent p-0 text-[14px] text-[var(--text-primary)] outline-none placeholder:text-[var(--text-tertiary)]"
						type="text"
						inputmode="decimal"
						pattern="[0-9]*\.?[0-9]*"
						placeholder="e.g. 42 or 3.14"
						bind:value={formRawValue}
						oninput={() => {
							formValueError = '';
						}}
						onkeydown={(e) => {
							if (e.key === 'Enter') void savePanel();
						}}
					/>
				</span>
				{#if formValueError}
					<span class="field-error">{formValueError}</span>
				{/if}
			</label>
		{:else if formValueType === CONFIG_VALUE_TYPE.Boolean}
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Value</span>
				<label class="toggle-row">
					<input
						type="checkbox"
						checked={formRawValue === 'true'}
						onchange={(e) => {
							formRawValue = e.currentTarget.checked ? 'true' : 'false';
						}}
					/>
					<span class="toggle-track">
						<span class="toggle-thumb"></span>
					</span>
					<span>{formRawValue === 'true' ? 'Enabled (true)' : 'Disabled (false)'}</span>
				</label>
			</div>
		{:else if formValueType === CONFIG_VALUE_TYPE.DateTime}
			<Input
				label="Value"
				type="datetime-local"
				bind:value={formRawValue}
				error={formValueError}
				className="mono"
				oninput={() => {
					formValueError = '';
				}}
			/>
		{:else if formValueType === CONFIG_VALUE_TYPE.TimeSpan}
			<Input
				label="Value"
				placeholder="HH:mm:ss"
				bind:value={formRawValue}
				error={formValueError}
				className="mono"
				oninput={() => {
					formValueError = '';
				}}
			/>
		{:else if formValueType === CONFIG_VALUE_TYPE.Json}
			<Textarea
				label="Value"
				placeholder={`{"key":"value"}`}
				bind:value={formRawValue}
				error={formValueError}
				className="mono"
				oninput={() => {
					formValueError = '';
				}}
			/>
		{:else if formValueType === CONFIG_VALUE_TYPE.Array}
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Value</span>
				<div class={`array-editor ${formValueError ? 'array-editor-error' : ''}`}>
					<div class="space-y-2">
						{#each formArrayItems as item, index}
							<div class="array-row">
								<input
									class="array-input mono"
									type="text"
									value={item}
									placeholder={`Item ${index + 1}`}
									oninput={(e) => updateFormArrayItem(index, e.currentTarget.value)}
								/>
								<Button variant="ghost" size="sm" className="array-icon-button" onclick={() => removeFormArrayItem(index)}>−</Button>
							</div>
						{/each}
						{#if formArrayItems.length === 0}
							<div class="array-empty">Empty array</div>
						{/if}
					</div>
					<div class="flex justify-start">
						<Button variant="secondary" size="sm" onclick={addFormArrayItem}>Add row</Button>
					</div>
				</div>
				{#if formValueError}
					<span class="field-error">{formValueError}</span>
				{/if}
			</div>
		{:else if formValueType === CONFIG_VALUE_TYPE.Enum}
			<div class="space-y-5">
				{#if enumValues().length > 0}
					<Select
						label="Value"
						bind:value={formRawValue}
						error={formValueError}
						onchange={() => {
							formValueError = '';
						}}
					>
						<option value="">Select value</option>
						{#each enumValues() as val}
							<option value={val}>{val}</option>
						{/each}
					</Select>
				{:else}
					<div class="rounded-[10px] border border-[var(--border)] bg-[var(--bg-subtle)] px-4 py-3 text-[13px] text-[var(--text-secondary)]">
						No enum values defined yet — enter value manually
					</div>
					<Input
						label="Value"
						bind:value={formRawValue}
						error={formValueError}
						className="mono"
						oninput={() => {
							formValueError = '';
						}}
					/>
				{/if}
			</div>
		{/if}

		{#if isMemberEditingExistingEntry}
			<div class="space-y-1.5">
				<span class="block text-[13px] text-[var(--text-secondary)]">Description</span>
				<div class="min-h-20 rounded-[8px] border border-[var(--border)] bg-[var(--bg-subtle)] px-3 py-2 text-[14px] text-[var(--text-primary)]">{formDescription || '-'}</div>
			</div>
		{:else}
			<Textarea id="entry-description" label="Description" bind:value={formDescription} />
		{/if}
	</div>

	{#snippet footerContent()}
		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => (showPanel = false)}>Cancel</Button>
			<Button loading={saving} onclick={savePanel}>Save entry</Button>
		</div>
	{/snippet}
</Modal>

<ConfirmDialog
	open={confirmDeleteOpen}
	title="Delete entry"
	message={pendingDelete ? `Delete config entry "${pendingDelete.key}"?` : ''}
	confirmLabel="Delete"
	loading={deleting}
	onconfirm={deleteEntry}
	oncancel={() => {
		confirmDeleteOpen = false;
		pendingDelete = null;
	}}
/>

<div class="fixed bottom-4 right-4 z-50 flex w-80 flex-col gap-2">
	{#each toasts as toast (toast.id)}
		<div class="rounded-[12px] border-l-4 border border-[var(--border)] border-l-[var(--accent)] bg-[var(--bg-surface)] px-4 py-3 text-[13px] shadow-[var(--shadow-md)]">{toast.message}</div>
	{/each}
</div>

<style>
	.version-carousel {
		overflow-x: auto;
		border-bottom: 1px solid var(--border);
		scrollbar-width: thin;
	}

	.version-tabs {
		display: inline-flex;
		min-width: 100%;
		gap: 4px;
		padding: 0 2px;
	}

	.version-tab {
		display: inline-flex;
		min-width: 148px;
		max-width: 220px;
		flex: 0 0 auto;
		flex-direction: column;
		justify-content: center;
		gap: 2px;
		border: 1px solid transparent;
		border-bottom: 0;
		border-radius: 8px 8px 0 0;
		padding: 10px 14px 11px;
		color: var(--text-secondary);
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			color 150ms ease;
	}

	.version-tab:hover {
		background: var(--bg-subtle);
		color: var(--text-primary);
	}

	.version-tab.active {
		border-color: var(--border);
		background: var(--bg-elevated);
		color: var(--accent-text);
		box-shadow: inset 0 2px 0 var(--accent);
	}

	.version-tab-label,
	.version-tab-date {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.version-tab-label {
		font-size: 14px;
		font-weight: 600;
	}

	.version-tab-date {
		font-size: 11px;
		color: var(--text-tertiary);
	}

	.bool-pill {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		padding: 2px 10px;
		border-radius: 20px;
		font-size: 12px;
		font-family: 'JetBrains Mono', monospace;
		font-weight: 500;
	}

	.bool-on {
		background: var(--success-subtle);
		color: var(--success);
	}

	.bool-off {
		background: var(--bg-muted);
		color: var(--text-secondary);
	}

	.bool-dot {
		width: 7px;
		height: 7px;
		border-radius: 50%;
		background: currentColor;
		flex-shrink: 0;
	}

	.toggle-inline,
	.toggle-row {
		display: inline-flex;
		align-items: center;
		gap: 10px;
		font-size: 13px;
		color: var(--text-primary);
	}

	.toggle-inline input,
	.toggle-row input {
		position: absolute;
		opacity: 0;
		pointer-events: none;
	}

	.toggle-track {
		position: relative;
		width: 40px;
		height: 22px;
		border-radius: 999px;
		background: var(--bg-muted);
		border: 1px solid var(--border);
		transition: background-color 150ms ease, border-color 150ms ease;
	}

	.toggle-thumb {
		position: absolute;
		top: 2px;
		left: 2px;
		width: 16px;
		height: 16px;
		border-radius: 50%;
		background: var(--bg-surface);
		box-shadow: var(--shadow-sm);
		transition: transform 150ms ease, background-color 150ms ease;
	}

	.toggle-inline input:checked + .toggle-track,
	.toggle-row input:checked + .toggle-track {
		background: var(--success-subtle);
		border-color: color-mix(in srgb, var(--success) 35%, var(--border));
	}

	.toggle-inline input:checked + .toggle-track .toggle-thumb,
	.toggle-row input:checked + .toggle-track .toggle-thumb {
		transform: translateX(18px);
		background: var(--success);
	}

	.toggle-label {
		font-family: 'JetBrains Mono', monospace;
		font-size: 12px;
	}

	.array-editor {
		display: grid;
		gap: 10px;
		width: min(520px, 100%);
	}

	.array-editor-inline {
		width: min(460px, 70vw);
	}

	.array-row {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 32px;
		gap: 8px;
		align-items: center;
	}

	.array-input {
		height: 36px;
		width: 100%;
		border-radius: 8px;
		border: 1px solid var(--accent);
		background: var(--bg-surface);
		padding: 0 10px;
		font-size: 13px;
		color: var(--text-primary);
		outline: none;
	}

	.array-editor-error .array-input {
		border-color: var(--danger);
	}

	.array-input::placeholder {
		color: var(--text-tertiary);
	}

	.field-error {
		display: block;
		font-size: 12px;
		color: var(--danger);
	}

	.array-icon-button {
		width: 32px;
		padding-left: 0;
		padding-right: 0;
		font-size: 18px;
	}

	.array-empty {
		border: 1px dashed var(--border);
		border-radius: 8px;
		background: var(--bg-subtle);
		padding: 9px 10px;
		font-size: 13px;
		color: var(--text-tertiary);
	}

	.array-preview {
		display: flex;
		max-width: 420px;
		flex-wrap: wrap;
		gap: 6px;
	}

	.array-chip,
	.array-more {
		max-width: 160px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		border-radius: 999px;
		border: 1px solid var(--border);
		background: var(--bg-subtle);
		padding: 2px 8px;
		font-size: 12px;
		color: var(--text-primary);
	}

	.array-more {
		color: var(--text-secondary);
	}
</style>

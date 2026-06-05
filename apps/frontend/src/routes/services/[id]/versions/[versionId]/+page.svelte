<script lang="ts">
	import { browser } from '$app/environment';
	import { PUBLIC_SIGNALR_URL } from '$env/static/public';
	import { page } from '$app/stores';
	import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
	import { onMount } from 'svelte';
	import { apiRequest, buildBackendUrl, CONFIG_VALUE_TYPE } from '$lib/api';
	import type {
		ApplicationServiceContract,
		ConfigEntryContract,
		ConfigValueType,
		ConfigVersionContract
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

	type Toast = { id: number; message: string };
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
	let entries = $state<ConfigEntry[]>([]);
	let loading = $state(true);
	let error = $state('');
	let actionError = $state('');

	let showPanel = $state(false);
	let panelMode = $state<'create' | 'edit'>('create');
	let saving = $state(false);
	let formId = $state('');
	let formKey = $state('');
	let formValueType = $state<ConfigValueType>(CONFIG_VALUE_TYPE.String);
	let formRawValue = $state('');
	let formEnumDefinition = $state('');
	let formDescription = $state('');
	let formGroupName = $state('');
	let formGroupDescription = $state('');

	let inlineId = $state('');
	let inlineValue = $state('');
	let pendingValues = $state<Record<string, string>>({});
	let showOnlyChanged = $state(false);
	let batchSaving = $state(false);

	let confirmDeleteOpen = $state(false);
	let deleting = $state(false);
	let pendingDelete: ConfigEntry | null = $state(null);

	let toasts = $state<Toast[]>([]);
	let toastSeed = 1;
	let connection: HubConnection | null = null;
	let collapsedGroups = $state<Record<string, boolean>>({});
	let changedRows = $state<Record<string, number>>({});
	let subscriptionPulse = $state(false);

	const serviceId = $derived($page.params.id);
	const versionId = $derived($page.params.versionId);
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

		if (normalized === original) delete next[entry.id];
		else next[entry.id] = normalized;

		pendingValues = next;
	}

	function discardPendingChanges() {
		pendingValues = {};
		showOnlyChanged = false;
		cancelInlineEdit();
	}

	function isEnumValueType(valueType: number | string): boolean {
		return normalizeValueType(valueType) === CONFIG_VALUE_TYPE.Enum;
	}

	function isBooleanValueType(valueType: number | string): boolean {
		return normalizeValueType(valueType) === CONFIG_VALUE_TYPE.Boolean;
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
		formEnumDefinition = '';
		formDescription = '';
		formGroupName = normalizedGroupName(groupName);
		formGroupDescription = knownGroups.find((group) => group.name === formGroupName)?.description ?? '';
		actionError = '';
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
		formEnumDefinition = entry.enumDefinition ?? '';
		formDescription = entry.description ?? '';
		formGroupName = normalizedGroupName(entry.groupName);
		formGroupDescription = entry.groupDescription?.trim() ?? knownGroups.find((g) => g.name === formGroupName)?.description ?? '';
		actionError = '';
		showPanel = true;
	}

	function markChanged(key: string) {
		changedRows = { ...changedRows, [key]: Date.now() };
		subscriptionPulse = true;
		setTimeout(() => {
			const next = { ...changedRows };
			delete next[key];
			changedRows = next;
		}, 1000);
		setTimeout(() => {
			subscriptionPulse = false;
		}, 1200);
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

	async function loadPage() {
		loading = true;
		error = '';
		try {
			const [serviceData, versionData] = await Promise.all([
				apiRequest<ServiceDetail>(`/services/${serviceId}`),
				apiRequest<VersionDetail>(`/configversions/${serviceId}/${versionId}`)
			]);
			const entryData = await apiRequest<ConfigEntry[]>(`/configentries/${serviceId}/${versionId}`);
			service = serviceData;
			version = versionData;
			entries = entryData.map((x) => ({ ...x, updatedAt: null }));
			pendingValues = {};
			showOnlyChanged = false;
			cancelInlineEdit();
			loadCollapsedState();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load entries';
		} finally {
			loading = false;
		}
	}

	async function savePanel() {
		actionError = '';
		const validation = validateValue(formValueType, formRawValue, formEnumDefinition);
		if (validation) {
			actionError = validation;
			return;
		}

		const normalizedGroup = normalizedGroupName(formGroupName);
		const normalizedDescription = normalizedGroup ? formGroupDescription.trim() : '';

		saving = true;
		try {
			if (panelMode === 'create') {
				const rawValue = normalizeRawValueForSave(formRawValue, formValueType);
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
				const rawValue = normalizeRawValueForSave(formRawValue, formValueType);
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
			actionError = e instanceof Error ? e.message : 'Failed to save entry';
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
			actionError = e instanceof Error ? e.message : 'Failed to delete entry';
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
	}

	function cancelInlineEdit() {
		inlineId = '';
		inlineValue = '';
	}

	function saveInline(entry: ConfigEntry) {
		if (!inlineId) return;
		const type = normalizeValueType(entry.valueType);
		const validation = validateValue(type, inlineValue, entry.enumDefinition ?? '');
		if (validation) {
			actionError = validation;
			return;
		}

		actionError = '';
		setPendingValue(entry, String(normalizeRawValueForSave(inlineValue, type)));
		cancelInlineEdit();
	}

	async function savePendingChanges() {
		if (!canBatchSave) return;

		actionError = '';
		for (const entry of pendingEntries) {
			const type = normalizeValueType(entry.valueType);
			const validation = validateValue(type, getEntryValue(entry), entry.enumDefinition ?? '');
			if (validation) {
				actionError = `${entry.key}: ${validation}`;
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
			showOnlyChanged = false;
			cancelInlineEdit();
			pushToast('Config changes saved');
		} catch (e) {
			actionError = e instanceof Error ? e.message : 'Failed to save changes';
		} finally {
			batchSaving = false;
		}
	}

	async function initRealtime() {
		if (!browser) return;

		const signalrUrl = PUBLIC_SIGNALR_URL || buildBackendUrl('/hubs/config');
		connection = new HubConnectionBuilder()
			.withUrl(signalrUrl, { withCredentials: true })
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Warning)
			.build();

		connection.on('ConfigChanged', (payload: any) => {
			const requests = (payload?.requests ?? payload?.Requests ?? []) as { id?: string; Id?: string; value?: string | null; Value?: string | null }[];
			if (requests.length === 0) {
				void loadPage();
				return;
			}

			let changed = 0;
			for (const request of requests) {
				const id = request.id ?? request.Id;
				if (!id) continue;

				const idx = entries.findIndex((entry) => entry.id === id);
				if (idx < 0) {
					void loadPage();
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

		await connection.start();
		await connection.invoke('JoinVersionGroup', serviceId, versionId);
	}

	async function teardownRealtime() {
		if (!connection) return;
		try {
			await connection.invoke('LeaveVersionGroup', serviceId, versionId);
		} catch {
			// no-op
		}
		await connection.stop();
		connection = null;
	}

	onMount(() => {
		void loadPage().then(() => initRealtime());
		return () => void teardownRealtime();
	});

	$effect(() => {
		if (formValueType === CONFIG_VALUE_TYPE.Boolean && formRawValue !== 'true' && formRawValue !== 'false') {
			formRawValue = 'false';
		}
	});
</script>

<section class={`space-y-6 ${pendingCount > 0 ? 'pb-28' : ''}`}>
	<nav class="text-[13px] text-[var(--text-secondary)]">
		<a class="hover:text-[var(--text-primary)]" href="/services">Services</a>
		<span class="mx-2">/</span>
		<a class="hover:text-[var(--text-primary)]" href={`/services/${serviceId}`}>{service?.name ?? '...'}</a>
		<span class="mx-2">/</span>
		<span class="text-[var(--text-primary)]">{version?.versionLabel ?? 'Version'}</span>
	</nav>

	<div class="page-header">
		<div>
			<div class="flex items-center gap-3">
				<h1 class="page-title">{version?.versionLabel ?? 'Config entries'}</h1>
				<span class={`status-dot ${subscriptionPulse ? 'pulse' : ''}`}></span>
			</div>
			<p class="page-subtitle">{version?.description ?? 'Versioned configuration entries for this service.'}</p>
		</div>
		<div class="flex items-center gap-3">
			<div class="rounded-[10px] border border-[var(--border)] bg-[var(--bg-surface)] px-3 py-2 text-[13px] text-[var(--text-secondary)]">
				<div class="text-[12px] text-[var(--text-tertiary)]">Version</div>
				<div class="mt-1 font-medium text-[var(--text-primary)]">{version?.versionLabel ?? 'Loading'}</div>
			</div>
			{#if userCanAll}
				<Button size="lg" onclick={() => openCreatePanel()}>Add entry</Button>
			{/if}
		</div>
	</div>

	{#if pendingCount > 0}
		<div class="flex flex-wrap items-center justify-between gap-3 rounded-[10px] border border-[var(--border)] bg-[var(--bg-surface)] px-4 py-3">
			<label class="inline-flex items-center gap-2 text-[13px] text-[var(--text-primary)]">
				<input class="h-4 w-4 accent-[var(--accent)]" type="checkbox" bind:checked={showOnlyChanged} />
				<span>Show changes</span>
			</label>
			<div class="text-[13px] text-[var(--text-secondary)]">
				{pendingCount} unsaved {pendingCount === 1 ? 'change' : 'changes'}
			</div>
		</div>
	{/if}

	{#if loading}
		<div class="space-y-4">
			<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
			<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
		</div>
	{:else if error}
		<p class="text-[13px] text-[var(--danger)]">{error}</p>
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
															<div class="flex items-start gap-2">
																<textarea
																	class="min-h-24 w-full rounded-[8px] border border-[var(--accent)] bg-[var(--bg-surface)] px-3 py-2 mono text-[13px] outline-none"
																	bind:value={inlineValue}
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
														{:else}
															<div class="flex items-center gap-2">
																<input
																	class="h-9 w-full rounded-[8px] border border-[var(--accent)] bg-[var(--bg-surface)] px-3 mono text-[13px] outline-none"
																	type="text"
																	inputmode={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? 'decimal' : undefined}
																	pattern={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? '[0-9]*\\.?[0-9]*' : undefined}
																	placeholder={normalizeValueType(entry.valueType) === CONFIG_VALUE_TYPE.Number ? 'e.g. 42 or 3.14' : undefined}
																	bind:value={inlineValue}
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

	{#if actionError}
		<p class="text-[13px] text-[var(--danger)]">{actionError}</p>
	{/if}
</section>

{#if pendingCount > 0}
	<div class="fixed bottom-4 left-1/2 z-40 w-[min(760px,calc(100vw-32px))] -translate-x-1/2 rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)] px-4 py-3 shadow-[var(--shadow-md)]">
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
			<Input label="Value" bind:value={formRawValue} className="mono" />
		{:else if formValueType === CONFIG_VALUE_TYPE.Number}
			<label class="block space-y-1.5 mono">
				<span class="block text-[13px] text-[var(--text-secondary)]">Value</span>
				<span class="flex h-9 items-center gap-2 rounded-[8px] border border-[var(--border)] bg-[var(--bg-surface)] px-3 transition-all duration-150 focus-within:border-[var(--accent)]">
					<input
						class="w-full border-0 bg-transparent p-0 text-[14px] text-[var(--text-primary)] outline-none placeholder:text-[var(--text-tertiary)]"
						type="text"
						inputmode="decimal"
						pattern="[0-9]*\.?[0-9]*"
						placeholder="e.g. 42 or 3.14"
						bind:value={formRawValue}
						onkeydown={(e) => {
							if (e.key === 'Enter') void savePanel();
						}}
					/>
				</span>
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
			<Input label="Value" type="datetime-local" bind:value={formRawValue} className="mono" />
		{:else if formValueType === CONFIG_VALUE_TYPE.TimeSpan}
			<Input label="Value" placeholder="HH:mm:ss" bind:value={formRawValue} className="mono" />
		{:else if formValueType === CONFIG_VALUE_TYPE.Json}
			<Textarea label="Value" placeholder={`{"key":"value"}`} bind:value={formRawValue} className="mono" />
		{:else if formValueType === CONFIG_VALUE_TYPE.Array}
			<Textarea label="Value" placeholder={`["first","second"]`} bind:value={formRawValue} className="mono" />
		{:else if formValueType === CONFIG_VALUE_TYPE.Enum}
			<div class="space-y-5">
				{#if enumValues().length > 0}
					<Select label="Value" bind:value={formRawValue}>
						<option value="">Select value</option>
						{#each enumValues() as val}
							<option value={val}>{val}</option>
						{/each}
					</Select>
				{:else}
					<div class="rounded-[10px] border border-[var(--border)] bg-[var(--bg-subtle)] px-4 py-3 text-[13px] text-[var(--text-secondary)]">
						No enum values defined yet — enter value manually
					</div>
					<Input label="Value" bind:value={formRawValue} className="mono" />
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
</style>

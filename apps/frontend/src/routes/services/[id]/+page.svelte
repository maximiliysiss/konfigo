<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { apiRequest, getApiErrorMessage } from '$lib/api';
	import type {
		ApplicationServiceContract,
		AuditLogContract,
		ConfigVersionContract,
		PageResponse
	} from '$lib/api';
	import { canAll, user } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Textarea from '$lib/components/ui/Textarea.svelte';
	import Badge from '$lib/components/ui/Badge.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import ErrorCallout from '../../../components/ui/ErrorCallout.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import { showToast } from '$lib/stores/toast';

	type TabKey = 'info' | 'members' | 'versions' | 'audit';
	type ServiceDetail = ApplicationServiceContract;
	type VersionDetail = ConfigVersionContract;
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

	let tab = $state<TabKey>('info');
	let service = $state<ServiceDetail | null>(null);
	let versions = $state<VersionDetail[]>([]);
	let audit = $state<AuditEntry[]>([]);
	let auditPage = $state(1);
	let auditPageSize = 10;
	let auditPageTokens = $state<string[]>(['']);
	let auditNextPageToken = $state('');
	let expandedRows = $state<Record<string, boolean>>({});

	let showEdit = $state(false);
	let editName = $state('');
	let editDescription = $state('');
	let editRepositoryUrl = $state('');
	let editContactEmail = $state('');
	let savingService = $state(false);
	let memberUserId = $state('');
	let addingMember = $state(false);
	let removingMember = $state<string | null>(null);

	let showNewVersionModal = $state(false);
	let newVersionLabel = $state('');
	let newVersionDescription = $state('');
	let basedOnVersionId = $state('');
	let creatingVersion = $state(false);

	let loading = $state(true);
	let error = $state('');
	let actionError = $state('');

	const userCanAll = $derived(canAll($user));

	function currentServiceId() {
		return $page.params.id ?? '';
	}

	function parseHashToTab(hash: string): TabKey {
		const key = hash.replace('#', '').toLowerCase();
		if (key === 'info' || key === 'members' || key === 'versions' || key === 'audit') return key;
		return 'info';
	}

	function versionTimestamp(version: VersionDetail): number {
		const parsed = new Date(version.createdAt).valueOf();
		return Number.isNaN(parsed) ? 0 : parsed;
	}

	function sortVersions(nextVersions: VersionDetail[]): VersionDetail[] {
		return [...nextVersions].sort((a, b) => versionTimestamp(b) - versionTimestamp(a));
	}

	function latestVersion(): VersionDetail | null {
		return versions[0] ?? null;
	}

	function serviceMembers(): string[] {
		return [...(service?.members ?? [])].sort((a, b) => a.localeCompare(b));
	}

	async function openLatestVersion() {
		const version = latestVersion();
		if (!version) {
			tab = 'versions';
			if (window.location.hash !== '#versions') window.history.replaceState(null, '', '#versions');
			return;
		}

		await goto(`/services/${currentServiceId()}/versions/${version.id}`);
	}

	function setTab(nextTab: TabKey) {
		if (nextTab === 'versions') {
			void openLatestVersion();
			return;
		}

		tab = nextTab;
		if (window.location.hash !== `#${nextTab}`) window.history.replaceState(null, '', `#${nextTab}`);
		if (nextTab === 'audit') void loadAudit(auditPage);
	}

	function prettyDate(value?: string | null): string {
		if (!value) return '-';
		const parsed = new Date(value);
		return Number.isNaN(parsed.valueOf()) ? '-' : parsed.toLocaleString();
	}

	async function loadService() {
		service = await apiRequest<ServiceDetail>(`/services/${currentServiceId()}`);
		editName = service.name;
		editDescription = service.description ?? '';
		editRepositoryUrl = service.repositoryUrl ?? '';
		editContactEmail = service.contactEmail ?? '';
	}

	async function loadVersions() {
		const raw = await apiRequest<ConfigVersionContract[]>(`/configversions/${currentServiceId()}`);
		versions = sortVersions(raw);
	}

	async function loadAudit(pageNumber = 1) {
		if (pageNumber > auditPage && auditNextPageToken) {
			auditPageTokens = [...auditPageTokens.slice(0, auditPage), auditNextPageToken];
		}
		auditPage = pageNumber;
		const payload = await apiRequest<PageResponse<AuditEntry>>(`/audit/${currentServiceId()}/search`, {
			method: 'POST',
			body: JSON.stringify({
				pageSize: auditPageSize,
				pageToken: auditPageTokens[auditPage - 1] || null
			})
		});
		audit = payload.entities;
		auditNextPageToken = payload.nextPageToken ?? '';
	}

	async function loadPage() {
		loading = true;
		error = '';
		try {
			await Promise.all([loadService(), loadVersions()]);
			if (tab === 'versions') {
				await openLatestVersion();
				return;
			}
			if (tab === 'audit') await loadAudit(auditPage);
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to load service');
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		setTab(parseHashToTab(window.location.hash));
		const onHashChange = () => {
			tab = parseHashToTab(window.location.hash);
			if (tab === 'versions') {
				void openLatestVersion();
				return;
			}
			if (tab === 'audit') void loadAudit(auditPage);
		};
		window.addEventListener('hashchange', onHashChange);
		void loadPage();
		return () => window.removeEventListener('hashchange', onHashChange);
	});

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
			await loadService();
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to save service');
		} finally {
			savingService = false;
		}
	}

	async function addMember() {
		if (!service || !memberUserId.trim()) return;
		actionError = '';
		addingMember = true;
		try {
			await apiRequest<unknown>(`/services/${service.id}/members?userId=${encodeURIComponent(memberUserId.trim())}`, {
				method: 'POST'
			});
			memberUserId = '';
			await loadService();
			showToast('Member added', 'success');
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to add member');
		} finally {
			addingMember = false;
		}
	}

	async function removeMember(userId: string) {
		if (!service) return;
		actionError = '';
		removingMember = userId;
		try {
			await apiRequest<unknown>(`/services/${service.id}/members?userId=${encodeURIComponent(userId)}`, {
				method: 'DELETE'
			});
			await loadService();
			showToast('Member removed', 'success');
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to remove member');
		} finally {
			removingMember = null;
		}
	}

	async function createVersion() {
		if (!service || !newVersionLabel.trim()) return;
		actionError = '';
		creatingVersion = true;
		try {
			await apiRequest<unknown>(`/configversions/${service.id}`, {
				method: 'POST',
				body: JSON.stringify({
					versionLabel: newVersionLabel.trim(),
					description: newVersionDescription,
				})
			});
			showNewVersionModal = false;
			newVersionLabel = '';
			newVersionDescription = '';
			basedOnVersionId = '';
			await loadVersions();
		} catch (e) {
			actionError = getApiErrorMessage(e, 'Failed to create version');
		} finally {
			creatingVersion = false;
		}
	}

	function toggleRow(index: number) {
		const key = `${auditPage}-${index}`;
		expandedRows = { ...expandedRows, [key]: !expandedRows[key] };
	}

	async function copyServiceId(serviceId: string) {
		try {
			await navigator.clipboard.writeText(serviceId);
			showToast('Service ID copied', 'success');
		} catch {
			showToast('Failed to copy service ID', 'error');
		}
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
</script>

{#if loading}
	<div class="space-y-4">
		<div class="h-8 w-64 animate-pulse rounded bg-[var(--bg-muted)]"></div>
		<div class="h-10 w-full animate-pulse rounded bg-[var(--bg-subtle)]"></div>
		<div class="h-64 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
	</div>
{:else if error}
	<ErrorCallout message={error} />
{:else if service}
	<section class="space-y-6">
		<div class="page-header">
			<div>
				<p class="section-label">Service</p>
				<h1 class="page-title">{service.name}</h1>
				<div class="service-id-pill mt-2">
					<span class="font-mono" title={service.id}>{service.id}</span>
					<button
						class="copy-id-button"
						type="button"
						aria-label="Copy service ID"
						title="Copy service ID"
						onclick={() => copyServiceId(service?.id ?? currentServiceId())}
					>
						<svg viewBox="0 0 16 16" class="h-3.5 w-3.5" fill="none" aria-hidden="true">
							<rect x="6" y="5" width="7" height="8" rx="1.5" stroke="currentColor" stroke-width="1.4" />
							<path d="M4 10.5H3.5A1.5 1.5 0 0 1 2 9V3.5A1.5 1.5 0 0 1 3.5 2H9a1.5 1.5 0 0 1 1.5 1.5V4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
						</svg>
					</button>
				</div>
				<p class="page-subtitle">{service.description ?? 'No service description provided.'}</p>
			</div>
			<div class="flex flex-wrap gap-2">
				<span class="metric-pill">{versions.length} versions</span>
				<span class="metric-pill">{service.members?.length ?? 0} members</span>
			</div>
		</div>

		<div class="flex flex-wrap gap-2">
			<button class={`tab-pill ${tab === 'info' ? 'active' : ''}`} onclick={() => setTab('info')}>Info</button>
			<button class={`tab-pill ${tab === 'members' ? 'active' : ''}`} onclick={() => setTab('members')}>Members</button>
			<button class={`tab-pill ${tab === 'versions' ? 'active' : ''}`} onclick={() => setTab('versions')}>Versions</button>
			<button class={`tab-pill ${tab === 'audit' ? 'active' : ''}`} onclick={() => setTab('audit')}>Audit</button>
		</div>

		<div class="pt-2">
			{#if tab === 'info'}
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
					{:else}
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
			{:else if tab === 'members'}
				<Card>
					{#snippet header()}
						<div class="flex flex-wrap items-center justify-between gap-3">
							<div>
								<p class="section-label">Access</p>
								<h2 class="mt-2 text-[20px] font-semibold">Members</h2>
							</div>
							<Badge>{serviceMembers().length} total</Badge>
						</div>
					{/snippet}

					<div class="space-y-6">
						{#if userCanAll}
							<form
								class="grid gap-3 md:grid-cols-[minmax(0,1fr)_auto]"
								onsubmit={(event) => {
									event.preventDefault();
									void addMember();
								}}
							>
								<Input label="User ID" placeholder="user@example.com or identity provider ID" bind:value={memberUserId} />
								<div class="flex items-end">
									<Button type="submit" loading={addingMember} disabled={!memberUserId.trim()}>Add member</Button>
								</div>
							</form>
						{/if}

						{#if serviceMembers().length === 0}
							<EmptyState title="No members yet" description="Add members to grant access to this service." />
						{:else}
							<div class="divide-y divide-[var(--border)] rounded-[8px] border border-[var(--border)] bg-[var(--bg-elevated)]">
								{#each serviceMembers() as member}
									<div class="flex flex-wrap items-center justify-between gap-3 p-4">
										<div class="min-w-0">
											<p class="break-words font-mono text-[13px] text-[var(--text-primary)]">{member}</p>
										</div>
										{#if userCanAll}
											<Button
												variant="danger"
												size="sm"
												loading={removingMember === member}
												disabled={removingMember !== null}
												onclick={() => removeMember(member)}
											>
												Remove
											</Button>
										{/if}
									</div>
								{/each}
							</div>
						{/if}
					</div>
				</Card>
			{:else if tab === 'versions'}
				<Card>
					{#snippet header()}
						<div class="flex items-center justify-between gap-3">
							<div>
								<p class="section-label">Config</p>
								<h2 class="mt-2 text-[20px] font-semibold">Versions</h2>
							</div>
							{#if userCanAll}
								<Button onclick={() => (showNewVersionModal = true)}>New version</Button>
							{/if}
						</div>
					{/snippet}

					<EmptyState title="No versions yet" description="Create the first version before adding configuration entries." />
				</Card>
			{:else}
				<Card>
					{#snippet header()}
						<div>
							<p class="section-label">History</p>
							<h2 class="mt-2 text-[20px] font-semibold">Audit log</h2>
						</div>
					{/snippet}

					{#if audit.length === 0}
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
{/if}

{#if userCanAll}
	<Modal title="Create version" open={showNewVersionModal} onclose={() => (showNewVersionModal = false)}>
		<div class="space-y-5">
			<Input label="Version label" placeholder="e.g. v1.2.0" bind:value={newVersionLabel} />
			<Textarea label="Description" bind:value={newVersionDescription} />
			<Select label="Based on version" bind:value={basedOnVersionId}>
				<option value="">None</option>
				{#each versions as version}
					<option value={version.id}>{version.versionLabel}</option>
				{/each}
			</Select>
		</div>

		{#snippet footerContent()}
			<div class="flex justify-end gap-3">
				<Button variant="ghost" onclick={() => (showNewVersionModal = false)}>Cancel</Button>
				<Button loading={creatingVersion} disabled={!newVersionLabel.trim()} onclick={createVersion}>Create version</Button>
			</div>
		{/snippet}
	</Modal>
{/if}

<style>
	.service-id-pill {
		display: inline-flex;
		max-width: 100%;
		align-items: center;
		gap: 6px;
		border: 1px solid var(--border);
		border-radius: 7px;
		background: var(--bg-subtle);
		padding: 3px 4px 3px 8px;
		color: var(--text-secondary);
		font-size: 11px;
		overflow-wrap: anywhere;
	}

	.copy-id-button {
		display: inline-flex;
		height: 22px;
		width: 22px;
		flex: 0 0 auto;
		align-items: center;
		justify-content: center;
		border-radius: 5px;
		color: var(--text-tertiary);
		transition:
			background-color 150ms ease,
			color 150ms ease;
	}

	.copy-id-button:hover {
		background: var(--bg-elevated);
		color: var(--text-primary);
	}
</style>

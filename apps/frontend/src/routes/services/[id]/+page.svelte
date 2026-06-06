<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { apiRequest } from '$lib/api';
	import type {
		ApplicationServiceContract,
		AuditLogContract,
		ConfigEntryContract,
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
	import Select from '$lib/components/ui/Select.svelte';

	type TabKey = 'info' | 'versions' | 'audit';
	type ServiceDetail = ApplicationServiceContract;
	type VersionWithStats = ConfigVersionContract & { entryCount: number };
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
	let versions = $state<VersionWithStats[]>([]);
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
	let editGitLabProjectId = $state('');
	let editContactEmail = $state('');
	let savingService = $state(false);

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
		return $page.params.id;
	}

	function parseHashToTab(hash: string): TabKey {
		const key = hash.replace('#', '').toLowerCase();
		if (key === 'info' || key === 'versions' || key === 'audit') return key;
		return 'info';
	}

	function setTab(nextTab: TabKey) {
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
		editGitLabProjectId = service.gitLabProjectId ?? '';
		editContactEmail = service.contactEmail ?? '';
	}

	async function loadVersions() {
		const raw = await apiRequest<ConfigVersionContract[]>(`/configversions/${currentServiceId()}`);
		const enriched = await Promise.all(
			raw.map(async (version) => {
				try {
					const entries = await apiRequest<ConfigEntryContract[]>(`/configentries/${currentServiceId()}/${version.id}`);
					return { ...version, entryCount: entries.length };
				} catch {
					return { ...version, entryCount: 0 };
				}
			})
		);
		versions = enriched;
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
			if (tab === 'audit') await loadAudit(auditPage);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load service';
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		setTab(parseHashToTab(window.location.hash));
		const onHashChange = () => {
			tab = parseHashToTab(window.location.hash);
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
					gitLabProjectId: editGitLabProjectId,
					contactEmail: editContactEmail
				})
			});
			showEdit = false;
			await loadService();
		} catch (e) {
			actionError = e instanceof Error ? e.message : 'Failed to save service';
		} finally {
			savingService = false;
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
			actionError = e instanceof Error ? e.message : 'Failed to create version';
		} finally {
			creatingVersion = false;
		}
	}

	function toggleRow(index: number) {
		const key = `${auditPage}-${index}`;
		expandedRows = { ...expandedRows, [key]: !expandedRows[key] };
	}

	async function openVersion(versionId: string) {
		await goto(`/services/${currentServiceId()}/versions/${versionId}`);
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
			gitLabProjectId: 'GitLab Project ID',
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
	<p class="text-[13px] text-[var(--danger)]">{error}</p>
{:else if service}
	<section class="space-y-6">
		<div class="page-header">
			<div>
				<p class="section-label">Service</p>
				<h1 class="page-title">{service.name}</h1>
				<p class="page-subtitle">{service.description ?? 'No service description provided.'}</p>
			</div>
			<div class="flex flex-wrap gap-2">
				<span class="metric-pill">{versions.length} versions</span>
			</div>
		</div>

		<div class="flex flex-wrap gap-2">
			<button class={`tab-pill ${tab === 'info' ? 'active' : ''}`} onclick={() => setTab('info')}>Info</button>
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
							<Input label="GitLab Project ID" bind:value={editGitLabProjectId} />
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
								<p class="text-[13px] text-[var(--text-secondary)]">GitLab Project ID</p>
								<p class="text-[14px] text-[var(--text-primary)]">{service.gitLabProjectId ?? '-'}</p>
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

					<div class="grid gap-3">
						{#each versions as version}
							<button class="rounded-[12px] border border-[var(--border)] bg-[var(--bg-elevated)] p-4 text-left transition-all duration-150 hover:-translate-y-[1px] hover:border-[var(--border-strong)]" type="button" onclick={() => openVersion(version.id)}>
								<div class="flex items-start justify-between gap-3">
									<div>
										<h3 class="text-[16px] font-semibold text-[var(--accent-text)]">{version.versionLabel}</h3>
										<p class="mt-2 text-[14px] text-[var(--text-secondary)]">{version.description ?? 'No description.'}</p>
									</div>
									<Badge variant="default">{version.entryCount} entries</Badge>
								</div>
								<p class="mt-4 text-[12px] text-[var(--text-tertiary)]">{prettyDate(version.createdAt)}</p>
							</button>
						{/each}
					</div>
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
			<p class="text-[13px] text-[var(--danger)]">{actionError}</p>
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

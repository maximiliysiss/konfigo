<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { fade } from 'svelte/transition';
	import { apiRequest, getApiErrorMessage } from '$lib/api';
	import type { ApplicationServiceContract, ConfigVersionContract } from '$lib/api';
	import ErrorCallout from '../../../components/ui/ErrorCallout.svelte';

	type TabKey = 'info' | 'members' | 'versions' | 'audit';
	type ServiceDetail = ApplicationServiceContract;
	type VersionDetail = ConfigVersionContract;

	let { children } = $props<{
		children: import('svelte').Snippet;
	}>();

	let service = $state<ServiceDetail | null>(null);
	let versions = $state<VersionDetail[]>([]);
	let loading = $state(true);
	let error = $state('');
	let openingVersions = $state(false);
	let mounted = false;
	let loadedServiceId = '';

	const serviceId = $derived($page.params.id ?? '');
	const activeTab = $derived.by<TabKey>(() => {
		if ($page.url.pathname.includes('/versions/')) return 'versions';
		const key = $page.url.hash.replace('#', '').toLowerCase();
		if (key === 'members' || key === 'versions' || key === 'audit') return key;
		return 'info';
	});

	function versionTimestamp(version: VersionDetail): number {
		const parsed = new Date(version.createdAt).valueOf();
		return Number.isNaN(parsed) ? 0 : parsed;
	}

	function sortVersions(nextVersions: VersionDetail[]): VersionDetail[] {
		return [...nextVersions].sort((a, b) => versionTimestamp(b) - versionTimestamp(a));
	}

	async function loadShell(nextServiceId = serviceId) {
		if (!nextServiceId) return;
		loading = true;
		error = '';
		try {
			const [serviceData, versionData] = await Promise.all([
				apiRequest<ServiceDetail>(`/services/${nextServiceId}`),
				apiRequest<VersionDetail[]>(`/configversions/${nextServiceId}`)
			]);
			service = serviceData;
			versions = sortVersions(versionData);
			loadedServiceId = nextServiceId;
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to load service');
		} finally {
			loading = false;
		}
	}

	async function openVersions() {
		if (!serviceId || openingVersions) return;
		openingVersions = true;
		try {
			let nextVersions = versions;
			if (loadedServiceId !== serviceId || nextVersions.length === 0) {
				nextVersions = sortVersions(await apiRequest<VersionDetail[]>(`/configversions/${serviceId}`));
				versions = nextVersions;
				loadedServiceId = serviceId;
			}

			const latest = nextVersions[0];
			if (latest) {
				await goto(`/services/${serviceId}/versions/${latest.id}`);
			} else {
				await goto(`/services/${serviceId}#versions`);
			}
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to load versions');
		} finally {
			openingVersions = false;
		}
	}

	function setTab(nextTab: TabKey) {
		if (nextTab === 'versions') {
			void openVersions();
			return;
		}
		void goto(`/services/${serviceId}#${nextTab}`);
	}

	async function copyServiceId() {
		try {
			await navigator.clipboard.writeText(service?.id ?? serviceId);
		} catch {
			// Clipboard access is optional; leave the UI unchanged if it is unavailable.
		}
	}

	onMount(() => {
		mounted = true;
		void loadShell();
		return () => {
			mounted = false;
		};
	});

	$effect(() => {
		if (!mounted || !serviceId || serviceId === loadedServiceId) return;
		void loadShell(serviceId);
	});

	$effect(() => {
		if (!mounted || loading || error || activeTab !== 'versions' || $page.url.pathname.includes('/versions/')) return;
		if (versions.length > 0) void openVersions();
	});
</script>

<section class="space-y-6">
	<div class="page-header">
		<div>
			<p class="section-label">Service</p>
			<h1 class="page-title">{service?.name ?? 'Service'}</h1>
			<div class="service-id-pill mt-2">
				<span class="font-mono" title={service?.id ?? serviceId}>{service?.id ?? serviceId}</span>
				<button class="copy-id-button" type="button" aria-label="Copy service ID" title="Copy service ID" onclick={copyServiceId}>
					<svg viewBox="0 0 16 16" class="h-3.5 w-3.5" fill="none" aria-hidden="true">
						<rect x="6" y="5" width="7" height="8" rx="1.5" stroke="currentColor" stroke-width="1.4" />
						<path d="M4 10.5H3.5A1.5 1.5 0 0 1 2 9V3.5A1.5 1.5 0 0 1 3.5 2H9a1.5 1.5 0 0 1 1.5 1.5V4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
					</svg>
				</button>
			</div>
			<p class="page-subtitle">{service?.description ?? 'No service description provided.'}</p>
		</div>
		<div class="flex flex-wrap gap-2">
			<span class="metric-pill">{versions.length} versions</span>
			<span class="metric-pill">{service?.members?.length ?? 0} members</span>
		</div>
	</div>

	<div class="flex flex-wrap gap-2">
		<button class={`tab-pill ${activeTab === 'info' ? 'active' : ''}`} type="button" onclick={() => setTab('info')}>Info</button>
		<button class={`tab-pill ${activeTab === 'members' ? 'active' : ''}`} type="button" onclick={() => setTab('members')}>Members</button>
		<button class={`tab-pill ${activeTab === 'versions' ? 'active' : ''}`} type="button" onclick={() => setTab('versions')}>Versions</button>
		<button class={`tab-pill ${activeTab === 'audit' ? 'active' : ''}`} type="button" onclick={() => setTab('audit')}>Audit</button>
	</div>

	{#if error}
		<ErrorCallout message={error} />
	{/if}

	<div class="service-tab-content" class:loading-tab={openingVersions}>
		{#if openingVersions}
			<div class="space-y-4">
				<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
				<div class="h-52 animate-pulse rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)]"></div>
			</div>
		{:else}
			{#key `${$page.url.pathname}${$page.url.hash}`}
				<div transition:fade={{ duration: 140 }}>
					{@render children()}
				</div>
			{/key}
		{/if}
	</div>
</section>

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

	.service-tab-content {
		transition: opacity 160ms ease, transform 160ms ease;
	}

	.service-tab-content.loading-tab {
		opacity: 0.82;
	}
</style>

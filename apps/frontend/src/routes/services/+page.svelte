<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { apiRequest } from '$lib/api';
	import type { ApplicationServiceContract, ConfigVersionContract, PageResponse } from '$lib/api';
	import { canAll, user } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import { showToast } from '$lib/stores/toast';

	type ServiceItem = ApplicationServiceContract & {
		versionCount?: number;
	};

	const pageSize = 12;
	let services = $state<ServiceItem[]>([]);
	let currentPage = $state(1);
	let pageTokens = $state<string[]>(['']);
	let nextPageToken = $state('');
	let search = $state('');
	let loading = $state(true);
	let error = $state('');
	let debounceHandle: ReturnType<typeof setTimeout> | null = null;
	let lastQuery = '';

	function parseQuery() {
		const params = $page.url.searchParams;
		search = params.get('name') ?? '';
		currentPage = 1;
		pageTokens = [''];
	}

	async function loadServices() {
		loading = true;
		error = '';
		try {
			const data = await apiRequest<PageResponse<ApplicationServiceContract>>('/services/search', {
				method: 'POST',
				body: JSON.stringify({
					name: search.trim() || null,
					pageSize,
					pageToken: pageTokens[currentPage - 1] || null
				})
			});
			nextPageToken = data.nextPageToken ?? '';

			const enriched = await Promise.all(
				data.entities.map(async (service) => {
					try {
						const versions = await apiRequest<ConfigVersionContract[]>(`/configversions/${service.id}`);
						return {
							...service,
							versionCount: versions.length
						};
					} catch {
						return {
							...service,
							versionCount: 0
						};
					}
				})
			);

			services = enriched;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load services';
			services = [];
		} finally {
			loading = false;
		}
	}

	onMount(async () => {
		lastQuery = $page.url.searchParams.toString();
		parseQuery();
		await loadServices();
	});

	$effect(() => {
		const query = $page.url.searchParams.toString();
		if (query === lastQuery) return;

		lastQuery = query;
		parseQuery();
		if (browser) void loadServices();
	});

	$effect(() => {
		if (!browser) return;
		if (debounceHandle) clearTimeout(debounceHandle);

		const currentName = $page.url.searchParams.get('name') ?? '';
		if (search === currentName) return;

		debounceHandle = setTimeout(async () => {
			const params = new URLSearchParams($page.url.searchParams);
			const trimmed = search.trim();
			if (trimmed) params.set('name', trimmed);
			else params.delete('name');

			await goto(`/services?${params.toString()}`, { replaceState: true, keepFocus: true, noScroll: true });
		}, 300);

		return () => {
			if (debounceHandle) clearTimeout(debounceHandle);
		};
	});

	async function goToPage(nextPage: number) {
		if (nextPage > currentPage && nextPageToken) {
			pageTokens = [...pageTokens.slice(0, currentPage), nextPageToken];
			currentPage = nextPage;
			await loadServices();
			return;
		}

		if (nextPage < currentPage && nextPage >= 1) {
			currentPage = nextPage;
			await loadServices();
		}
	}

	async function copyServiceId(event: MouseEvent, serviceId: string) {
		event.preventDefault();
		event.stopPropagation();

		try {
			await navigator.clipboard.writeText(serviceId);
			showToast('Service ID copied', 'success');
		} catch {
			showToast('Failed to copy service ID', 'error');
		}
	}

	function formatServiceDate(value: string | null | undefined): string {
		if (!value) return 'Never';
		return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value));
	}

	const userCanAll = $derived(canAll($user));
</script>

<section class="space-y-6">
	<div class="page-header border-l-2 border-[var(--ink)] pl-5">
		<div class="max-w-[620px]">
			<p class="section-label">Inventory</p>
			<h1 class="page-title mt-2">Services</h1>
			<p class="page-subtitle">Browse every service, inspect ownership, and jump into versioned configuration.</p>
		</div>
		{#if userCanAll}
			<a href="/services/new">
				<Button size="lg">New service</Button>
			</a>
		{/if}
	</div>

	<Card className="border-[var(--border-strong)]">
		<Input id="service-search" label="Search services" placeholder="Filter by service name" bind:value={search}>
			{#snippet prefix()}
				<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
					<circle cx="7" cy="7" r="4.5" stroke="currentColor" stroke-width="1.5" />
					<path d="M10.5 10.5 14 14" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
				</svg>
			{/snippet}
		</Input>
	</Card>

	{#if loading}
		<div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
			{#each Array(6) as _, i}
				<Card className="animate-pulse">
					<div class="space-y-4">
						<div class="h-5 w-2/3 rounded bg-[var(--bg-muted)]"></div>
						<div class="h-4 w-full rounded bg-[var(--bg-subtle)]"></div>
						<div class="h-4 w-4/5 rounded bg-[var(--bg-subtle)]"></div>
						<div class="flex gap-2">
							<div class="h-7 w-24 rounded-full bg-[var(--bg-subtle)]"></div>
							<div class="h-7 w-24 rounded-full bg-[var(--bg-subtle)]"></div>
						</div>
					</div>
				</Card>
			{/each}
		</div>
	{:else if error}
		<p class="text-[13px] text-[var(--danger)]">{error}</p>
	{:else if services.length === 0}
		{#if userCanAll}
			<EmptyState title="No services found" description="Try a different search term or create a new service to start managing configuration.">
				{#snippet icon()}
					<svg viewBox="0 0 48 48" class="h-12 w-12" fill="none" aria-hidden="true">
						<rect x="8" y="10" width="32" height="24" rx="6" stroke="currentColor" stroke-width="2" />
						<path d="M16 20h16M16 26h10" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
					</svg>
				{/snippet}
				{#snippet action()}
					<a href="/services/new">
						<Button>Create service</Button>
					</a>
				{/snippet}
			</EmptyState>
		{:else}
			<EmptyState title="No services found" description="Try a different search term or create a new service to start managing configuration.">
				{#snippet icon()}
					<svg viewBox="0 0 48 48" class="h-12 w-12" fill="none" aria-hidden="true">
						<rect x="8" y="10" width="32" height="24" rx="6" stroke="currentColor" stroke-width="2" />
						<path d="M16 20h16M16 26h10" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
					</svg>
				{/snippet}
			</EmptyState>
		{/if}
	{:else}
		<div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
			{#each services as service}
				<a href={`/services/${service.id}`} class="group block h-full">
					<Card className="service-card h-full">
						<div class="service-card-content">
							<div class="service-card-topline">
								<div class="service-avatar" aria-hidden="true">
									<span>{service.name.slice(0, 2).toUpperCase()}</span>
								</div>
								<div class="service-card-heading">
									<h2 class="service-title">{service.name}</h2>
									<p class="service-updated">Updated {formatServiceDate(service.updatedAt ?? service.createdAt)}</p>
								</div>
							</div>

							<div class="service-card-main">
								<p class="service-description">{service.description ?? 'No description provided.'}</p>
							</div>

							<div class="service-summary-row">
								<div class="service-summary-item">
									<span class="service-meta-label">Versions</span>
									<strong>{service.versionCount ?? 0}</strong>
								</div>
								<div class="service-summary-item">
									<span class="service-meta-label">Created</span>
									<strong class="service-date-value">{formatServiceDate(service.createdAt)}</strong>
								</div>
							</div>

							<div class="service-detail-list">
								<div class="service-detail-item">
									<span class="service-detail-icon" aria-hidden="true">
										<svg viewBox="0 0 16 16" class="h-3.5 w-3.5" fill="none">
											<path d="M6.5 3.5H5A2.5 2.5 0 0 0 2.5 6v4A2.5 2.5 0 0 0 5 12.5h1.5M9.5 3.5H11A2.5 2.5 0 0 1 13.5 6v4A2.5 2.5 0 0 1 11 12.5H9.5M6 8h4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
										</svg>
									</span>
									<div class="service-detail-copy">
										<span class="service-meta-label">Repository</span>
										{#if service.repositoryUrl}
											<span class="service-detail-value" title={service.repositoryUrl}>{service.repositoryUrl}</span>
										{:else}
											<span class="service-empty-meta">Not linked</span>
										{/if}
									</div>
								</div>
								<div class="service-detail-item">
									<span class="service-detail-icon" aria-hidden="true">
										<svg viewBox="0 0 16 16" class="h-3.5 w-3.5" fill="none">
											<path d="M2.5 5.5 8 9l5.5-3.5M4 4h8a1.5 1.5 0 0 1 1.5 1.5v5A1.5 1.5 0 0 1 12 12H4a1.5 1.5 0 0 1-1.5-1.5v-5A1.5 1.5 0 0 1 4 4Z" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" />
										</svg>
									</span>
									<div class="service-detail-copy">
										<span class="service-meta-label">Contact</span>
										{#if service.contactEmail}
											<span class="service-detail-value" title={service.contactEmail}>{service.contactEmail}</span>
										{:else}
											<span class="service-empty-meta">Not specified</span>
										{/if}
									</div>
								</div>
							</div>

							<div class="service-card-footer">
								<div class="service-id-pill">
									<svg viewBox="0 0 16 16" class="h-3.5 w-3.5 flex-none" fill="none" aria-hidden="true">
										<path d="M5.5 3.5h5A2.5 2.5 0 0 1 13 6v4a2.5 2.5 0 0 1-2.5 2.5h-5A2.5 2.5 0 0 1 3 10V6a2.5 2.5 0 0 1 2.5-2.5Z" stroke="currentColor" stroke-width="1.4" />
										<path d="M6 7.75h4M6 10h2.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
									</svg>
									<span class="truncate font-mono" title={service.id}>{service.id}</span>
									<button
										class="copy-id-button"
										type="button"
										aria-label="Copy service ID"
										title="Copy service ID"
										onclick={(event) => copyServiceId(event, service.id)}
									>
										<svg viewBox="0 0 16 16" class="h-3.5 w-3.5" fill="none" aria-hidden="true">
											<rect x="6" y="5" width="7" height="8" rx="1.5" stroke="currentColor" stroke-width="1.4" />
											<path d="M4 10.5H3.5A1.5 1.5 0 0 1 2 9V3.5A1.5 1.5 0 0 1 3.5 2H9a1.5 1.5 0 0 1 1.5 1.5V4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" />
										</svg>
									</button>
								</div>
								<span class="open-indicator" aria-hidden="true">
									<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none">
										<path d="M5 3.5 9.5 8 5 12.5" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" />
									</svg>
								</span>
							</div>
						</div>
					</Card>
				</a>
			{/each}
		</div>

		<div class="flex items-center justify-between gap-4">
				<p class="text-[13px] text-[var(--text-secondary)]">Page {currentPage}</p>
				<div class="flex gap-3">
					<Button variant="secondary" disabled={currentPage <= 1} onclick={() => goToPage(currentPage - 1)}>Previous</Button>
					<Button variant="secondary" disabled={!nextPageToken} onclick={() => goToPage(currentPage + 1)}>Next</Button>
				</div>
		</div>
	{/if}
</section>

<style>
	.service-id-pill {
		display: inline-flex;
		max-width: 100%;
		align-items: center;
		gap: 6px;
		min-width: 0;
		border: 1px solid color-mix(in srgb, var(--border-strong) 54%, transparent);
		border-radius: 7px;
		background: color-mix(in srgb, var(--bg-subtle) 76%, transparent);
		padding: 3px 4px 3px 8px;
		color: var(--text-secondary);
		font-size: 11px;
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

	:global(.service-card) {
		position: relative;
		height: 100%;
		border-color: color-mix(in srgb, var(--border) 82%, transparent);
		background:
			linear-gradient(135deg, color-mix(in srgb, var(--bg-elevated) 92%, transparent), color-mix(in srgb, var(--bg-surface) 96%, transparent)),
			var(--bg-surface);
		transition:
			border-color 180ms ease,
			box-shadow 180ms ease,
			transform 180ms ease;
	}

	:global(.group:hover .service-card),
	:global(.group:focus-visible .service-card) {
		transform: translateY(-3px);
		border-color: color-mix(in srgb, var(--accent) 62%, var(--border-strong));
		box-shadow: 0 20px 44px color-mix(in srgb, var(--ink) 12%, transparent);
	}

	.service-card-content {
		position: relative;
		display: flex;
		min-height: 318px;
		height: 100%;
		flex-direction: column;
		gap: 16px;
	}

	.service-card-topline,
	.service-card-footer {
		position: relative;
		display: flex;
		align-items: flex-start;
		gap: 12px;
		min-width: 0;
	}

	.service-avatar {
		width: 42px;
		height: 42px;
		flex: 0 0 auto;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 8px;
		border: 1px solid color-mix(in srgb, var(--accent) 34%, var(--border));
		background:
			linear-gradient(145deg, color-mix(in srgb, var(--accent-subtle) 78%, var(--bg-elevated)), var(--bg-elevated));
		color: var(--accent-text);
		font-family: 'JetBrains Mono', monospace;
		font-size: 13px;
		font-weight: 700;
		letter-spacing: 0;
		box-shadow: inset 0 1px 0 color-mix(in srgb, white 52%, transparent);
	}

	.service-card-heading {
		min-width: 0;
		flex: 1 1 auto;
		padding-top: 1px;
	}

	.service-card-main {
		position: relative;
		min-width: 0;
	}

	.service-title {
		font-family: 'JetBrains Mono', monospace;
		font-size: 17px;
		line-height: 1.35;
		font-weight: 700;
		overflow-wrap: anywhere;
	}

	.service-updated {
		margin-top: 3px;
		color: var(--text-tertiary);
		font-size: 12px;
	}

	.service-description {
		display: -webkit-box;
		color: var(--text-secondary);
		font-size: 14px;
		line-height: 1.55;
		line-clamp: 2;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	.service-summary-row {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 10px;
	}

	.service-summary-item {
		min-height: 68px;
		min-width: 0;
		display: flex;
		flex-direction: column;
		justify-content: center;
		gap: 5px;
		border: 1px solid color-mix(in srgb, var(--border) 78%, transparent);
		border-radius: 8px;
		background: color-mix(in srgb, var(--bg-subtle) 56%, transparent);
		padding: 10px;
	}

	.service-meta-label {
		color: var(--text-tertiary);
		font-size: 10px;
		font-weight: 700;
		letter-spacing: 0.12em;
		text-transform: uppercase;
	}

	.service-summary-item strong {
		color: var(--text-primary);
		font-size: 22px;
		line-height: 1;
	}

	.service-date-value {
		font-family: 'Inter', sans-serif;
		font-size: 13px !important;
		font-weight: 700;
		line-height: 1.25 !important;
	}

	.service-detail-list {
		display: flex;
		flex-direction: column;
		gap: 8px;
	}

	.service-detail-item {
		display: flex;
		min-width: 0;
		align-items: center;
		gap: 10px;
		border: 1px solid color-mix(in srgb, var(--border) 66%, transparent);
		border-radius: 8px;
		background: color-mix(in srgb, var(--bg-elevated) 66%, transparent);
		padding: 9px 10px;
	}

	.service-detail-icon {
		width: 28px;
		height: 28px;
		flex: 0 0 auto;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 7px;
		background: var(--bg-subtle);
		color: var(--text-secondary);
	}

	.service-detail-copy {
		min-width: 0;
		display: flex;
		flex: 1 1 auto;
		flex-direction: column;
		gap: 2px;
	}

	.service-detail-value,
	.service-empty-meta {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		color: var(--accent-text);
		font-size: 12px;
		font-weight: 600;
	}

	.service-empty-meta {
		color: var(--text-tertiary);
	}

	.service-card-footer {
		margin-top: auto;
		padding-top: 2px;
	}

	.open-indicator {
		width: 32px;
		height: 32px;
		flex: 0 0 auto;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 7px;
		border: 1px solid var(--border);
		background: var(--bg-elevated);
		color: var(--text-secondary);
		transition:
			transform 180ms ease,
			border-color 180ms ease,
			color 180ms ease;
	}

	:global(.group:hover) .open-indicator,
	:global(.group:focus-visible) .open-indicator {
		transform: translateX(2px);
		border-color: var(--accent);
		color: var(--accent-text);
	}

	@media (max-width: 420px) {
		.service-summary-row {
			grid-template-columns: 1fr;
		}

		.service-card-footer {
			align-items: stretch;
		}

		.open-indicator {
			display: none;
		}
	}
</style>

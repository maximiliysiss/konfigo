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
	import Badge from '$lib/components/ui/Badge.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';

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

	const userCanAll = $derived(canAll($user));
</script>

<section class="space-y-6">
	<div class="page-header">
		<div>
			<h1 class="page-title">Services</h1>
			<p class="page-subtitle">Browse every service, inspect ownership, and jump into versioned configuration.</p>
		</div>
		{#if userCanAll}
			<a href="/services/new">
				<Button size="lg">New service</Button>
			</a>
		{/if}
	</div>

	<Card>
		<Input id="service-search" label="Search services" placeholder="Search by service name" bind:value={search}>
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
				<a href={`/services/${service.id}`} class="group block">
					<Card className="h-full transition-all duration-150 group-hover:-translate-y-[1px] group-hover:border-[var(--border-strong)]">
						<div class="flex h-full flex-col gap-4">
							<div class="flex items-start justify-between gap-3">
								<div>
									<h2 class="text-[16px] font-semibold">{service.name}</h2>
									<p class="mt-2 line-clamp-2 text-[14px] text-[var(--text-secondary)]">{service.description ?? 'No description provided.'}</p>
								</div>
								<Badge variant="success">Active</Badge>
							</div>

							<div class="text-[13px] text-[var(--text-secondary)]">
								{#if service.repositoryUrl}
									<div class="flex items-center gap-2">
										<svg viewBox="0 0 16 16" class="h-4 w-4 text-[var(--text-tertiary)]" fill="none" aria-hidden="true">
											<path d="M6 10.5 10 6.5M5 6.5H4a2 2 0 0 0 0 4h1M11 6.5h1a2 2 0 0 1 0 4h-1" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
										</svg>
										<span class="truncate text-[var(--accent-text)]">{service.repositoryUrl}</span>
									</div>
								{:else}
									<span class="text-[var(--text-tertiary)]">No repository linked</span>
								{/if}
							</div>

							<div class="mt-auto flex items-center justify-between gap-3">
								<div class="flex flex-wrap gap-2">
									<span class="metric-pill">{service.versionCount ?? 0} versions</span>
								</div>
								<Button variant="ghost" size="sm">View</Button>
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
	.line-clamp-2 {
		display: -webkit-box;
		line-clamp: 2;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}
</style>

<script lang="ts">
	import { fade } from 'svelte/transition';
	import { page } from '$app/stores';
	import '../app.css';
	import { buildBackendUrl } from '$lib/api';
	import { canAll, user, type AuthUser } from '$lib/stores/auth';
	import { consumeQueuedToast, toasts } from '$lib/stores/toast';
	import Button from '$lib/components/ui/Button.svelte';
	import Toast from '$lib/components/ui/Toast.svelte';
	import { onMount } from 'svelte';

	type ConnectionError = {
		message: string;
		status?: number;
	};

	let { data, children } = $props<{
		data: { user: AuthUser | null; connectionError: ConnectionError | null };
		children: import('svelte').Snippet;
	}>();

	$effect(() => {
		user.set(data.connectionError ? null : data.user);
	});

	onMount(() => {
		consumeQueuedToast();
	});

	const currentUser = $derived(data.user);
	const showShell = $derived($page.url.pathname !== '/login' && currentUser !== null);
	const currentPath = $derived($page.url.pathname);
	const pageTransitionKey = $derived.by(() => {
		const match = currentPath.match(/^\/services\/([^/]+)\/versions\/[^/]+$/);
		if (match) return `/services/${match[1]}/versions`;
		return currentPath;
	});
	const userCanAll = $derived(canAll(currentUser));
	const currentUserLabel = $derived(currentUser?.email ?? currentUser?.name ?? currentUser?.id ?? '');
	const signOutHref = $derived(buildBackendUrl('/auth/logout?returnUrl=/login'));

	const navItems = $derived.by(() => {
		const items: { href: string; label: string; icon: 'grid' | 'plus' }[] = [
			{ href: '/services', label: 'Services', icon: 'grid' }
		];
		if (userCanAll) {
			items.push({ href: '/services/new', label: 'New Service', icon: 'plus' });
		}
		return items;
	});

	function icon(name: 'grid' | 'plus' | 'log-out') {
		if (name === 'plus') {
			return 'M8 3v10M3 8h10';
		}
		if (name === 'log-out') {
			return 'M6 3H4a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h2M10 12l3-4-3-4M13 8H7';
		}
		return 'M3 3h4v4H3zM9 3h4v4H9zM3 9h4v4H3zM9 9h4v4H9z';
	}

	function retryConnection() {
		window.location.reload();
	}
</script>

<svelte:head>
	<link rel="icon" type="image/png" href="/app-icon.png" />
	<link rel="apple-touch-icon" href="/app-icon.png" />
	<title>Konfigo</title>
</svelte:head>

{#if data.connectionError}
	<div class="connection-background">
		<section class="connection-status surface">
			<div class="connection-status-icon" aria-hidden="true">
				<svg viewBox="0 0 48 48" class="h-12 w-12" fill="none">
					<path d="M14 29.5a8 8 0 0 1 11.3 0M8.5 23a17 17 0 0 1 22.8-1.2M31 9.5a29 29 0 0 1 8.5 6.2" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" />
					<path d="M39 29.5 29.5 39M29.5 29.5 39 39" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" />
					<circle cx="19.5" cy="37" r="2" fill="currentColor" />
				</svg>
			</div>
			<p class="section-label">Connection unavailable</p>
			<h1 class="connection-status-title">Cannot connect to Konfigo</h1>
			<p class="connection-status-message">{data.connectionError.message}</p>
			{#if data.connectionError.status}
				<p class="connection-status-detail">Backend health check returned HTTP {data.connectionError.status}.</p>
			{/if}
			<div class="connection-status-actions">
				<Button size="lg" onclick={retryConnection}>Retry</Button>
			</div>
		</section>
	</div>
{:else if showShell}
	<div class="app-shell">
		<header class="app-topbar">
			<div class="topbar-left">
				<a class="logo-mark" href="/services">
					<img src="/app-icon.png" alt="" class="logo-mark-image" />
					<span>Konfigo</span>
				</a>

				<nav class="topbar-nav">
					{#each navItems as item}
						<a class={`topbar-nav-item ${currentPath === item.href ? 'active' : ''}`} href={item.href}>
							<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
								<path d={icon(item.icon)} stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
							</svg>
							<span>{item.label}</span>
						</a>
					{/each}
				</nav>
			</div>

			<div class="topbar-auth">
				<div class="user-identity" title={currentUserLabel}>
					<div class="avatar-circle" aria-hidden="true">
						<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
							<circle cx="8" cy="6" r="2.5" stroke="currentColor" stroke-width="1.5" />
							<path d="M3 13.5c.8-2 2.7-3.2 5-3.2s4.2 1.2 5 3.2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
						</svg>
					</div>
					<span class="user-identity-label">{currentUserLabel}</span>
				</div>
				<a class="topbar-signout" href={signOutHref}>
					<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
						<path d={icon('log-out')} stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
					</svg>
					<span>Sign out</span>
				</a>
			</div>
		</header>

		<div class="app-main">
			<main class="app-content">
				{#key pageTransitionKey}
					<div class="page-fade" transition:fade={{ duration: 150 }}>
						{@render children()}
					</div>
				{/key}
			</main>
		</div>
	</div>
{:else}
	<div class="page-fade" transition:fade={{ duration: 150 }}>
		{@render children()}
	</div>
{/if}

<div class="fixed bottom-4 right-4 z-50 flex w-[320px] flex-col gap-3">
	{#each $toasts as toast (toast.id)}
		<Toast {toast} />
	{/each}
</div>

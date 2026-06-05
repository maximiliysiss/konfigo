<script lang="ts">
	import { fade } from 'svelte/transition';
	import { page } from '$app/stores';
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { buildBackendUrl } from '$lib/api';
	import { canAll, user, type AuthUser } from '$lib/stores/auth';
	import { consumeQueuedToast, toasts } from '$lib/stores/toast';
	import Toast from '$lib/components/ui/Toast.svelte';
	import { onMount } from 'svelte';

	let { data, children } = $props<{
		data: { user: AuthUser | null };
		children: import('svelte').Snippet;
	}>();

	$effect(() => {
		user.set(data.user);
	});

	onMount(() => {
		consumeQueuedToast();
	});

	const currentUser = $derived(data.user);
	const showShell = $derived($page.url.pathname !== '/login' && currentUser !== null);
	const currentPath = $derived($page.url.pathname);
	const userCanAll = $derived(canAll(currentUser));
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
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<title>Realtime Config Manager</title>
</svelte:head>

{#if showShell}
	<div class="app-shell">
		<header class="app-topbar">
			<div class="topbar-left">
				<a class="logo-mark" href="/services">Realtime Config</a>

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
				<div class="avatar-circle" aria-hidden="true">
					<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
						<circle cx="8" cy="6" r="2.5" stroke="currentColor" stroke-width="1.5" />
						<path d="M3 13.5c.8-2 2.7-3.2 5-3.2s4.2 1.2 5 3.2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
					</svg>
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
				{#key currentPath}
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

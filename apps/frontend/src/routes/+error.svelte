<script lang="ts">
	import { page } from '$app/stores';
	import Button from '$lib/components/ui/Button.svelte';

	function retry() {
		window.location.reload();
	}

	const message = $derived(
		$page.error?.message?.trim() ||
			'The app could not load this page. Check your network connection and backend availability, then try again.'
	);
</script>

<svelte:head>
	<title>Connection unavailable</title>
</svelte:head>

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
		<p class="connection-status-message">{message}</p>
		<div class="connection-status-actions">
			<Button size="lg" onclick={retry}>Retry</Button>
		</div>
	</section>
</div>

<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { jwtToken } from '$lib/stores/auth';
	import { onMount } from 'svelte';

	let error = $state<string | null>(null);

	onMount(async () => {
		const code = $page.url.searchParams.get('code');
		const state = $page.url.searchParams.get('state');
		const errorParam = $page.url.searchParams.get('error');

		if (errorParam) {
			error = $page.url.searchParams.get('error_description') ?? errorParam;
			return;
		}

		if (!code) {
			error = 'No authorization code received.';
			return;
		}

		const savedState = sessionStorage.getItem('oauth_state');
		const codeVerifier = sessionStorage.getItem('oauth_code_verifier');
		const tokenUrl = sessionStorage.getItem('oauth_token_url');
		const clientId = sessionStorage.getItem('oauth_client_id');
		const redirectUri = sessionStorage.getItem('oauth_redirect_uri');
		const returnUrl = sessionStorage.getItem('oauth_return_url') ?? '/services';

		sessionStorage.removeItem('oauth_state');
		sessionStorage.removeItem('oauth_code_verifier');
		sessionStorage.removeItem('oauth_token_url');
		sessionStorage.removeItem('oauth_client_id');
		sessionStorage.removeItem('oauth_redirect_uri');
		sessionStorage.removeItem('oauth_return_url');

		if (!state || state !== savedState) {
			error = 'Invalid state parameter. Please try signing in again.';
			return;
		}

		if (!tokenUrl || !clientId || !codeVerifier || !redirectUri) {
			error = 'Missing session data. Please try signing in again.';
			return;
		}

		try {
			const response = await fetch(tokenUrl, {
				method: 'POST',
				headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
				body: new URLSearchParams({
					grant_type: 'authorization_code',
					code,
					redirect_uri: redirectUri,
					client_id: clientId,
					code_verifier: codeVerifier
				})
			});

			if (!response.ok) {
				const text = await response.text();
				error = `Token exchange failed: ${text || response.statusText}`;
				return;
			}

			const tokens = (await response.json()) as { access_token?: string };
			if (!tokens.access_token) {
				error = 'No access token in response.';
				return;
			}

			jwtToken.set(tokens.access_token);
			await goto(returnUrl, { replaceState: true });
		} catch (e) {
			error = e instanceof Error ? e.message : 'Unexpected error during token exchange.';
		}
	});
</script>

<svelte:head>
	<title>Signing in… - Konfigo</title>
</svelte:head>

<main class="login-background">
	<div class="w-full max-w-[420px] rounded-[8px] border border-[var(--border)] bg-[var(--bg-surface)] p-7 shadow-[var(--shadow-md)]">
		<div class="mb-5 flex items-center gap-3">
			<img src="/app-icon.png" alt="" class="h-12 w-12 rounded-[10px] border border-[var(--border)] bg-[var(--bg-elevated)] object-cover" />
			<div>
				<p class="section-label">Konfigo</p>
				<h1 class="mt-1 text-[22px] font-semibold text-[var(--text-primary)]">
					{#if error}Sign in failed{:else}Signing in…{/if}
				</h1>
			</div>
		</div>

		{#if error}
			<p class="text-[14px] text-[var(--color-danger)]">{error}</p>
			<a class="login-sso-button mt-6" href="/login">Try again</a>
		{:else}
			<p class="text-[14px] text-[var(--text-secondary)]">Completing authentication, please wait…</p>
		{/if}
	</div>
</main>

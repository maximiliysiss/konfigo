<script lang="ts">
	import { page } from '$app/stores';
	import { buildBackendUrl } from '$lib/api';
	import type { AuthConfig } from '$lib/api';

	type Data = { authConfig: AuthConfig };
	let { data } = $props<{ data: Data }>();

	const returnUrl = $derived.by(() => {
		const value = $page.url.searchParams.get('returnUrl');
		return value && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\')
			? value
			: '/services';
	});

	const loginHref = $derived(buildBackendUrl(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`));

	function base64UrlEncode(bytes: Uint8Array): string {
		return btoa(String.fromCharCode(...bytes))
			.replace(/\+/g, '-')
			.replace(/\//g, '_')
			.replace(/=/g, '');
	}

	async function startJwtLogin() {
		const jwt = data.authConfig.jwt;
		if (!jwt) return;

		const verifier = base64UrlEncode(crypto.getRandomValues(new Uint8Array(32)));
		const challenge = base64UrlEncode(
			new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier)))
		);
		const state = crypto.randomUUID();
		const redirectUri = window.location.origin + '/callback';

		sessionStorage.setItem('oauth_state', state);
		sessionStorage.setItem('oauth_code_verifier', verifier);
		sessionStorage.setItem('oauth_token_url', jwt.tokenUrl);
		sessionStorage.setItem('oauth_client_id', jwt.clientId);
		sessionStorage.setItem('oauth_redirect_uri', redirectUri);
		sessionStorage.setItem('oauth_return_url', returnUrl);

		const params = new URLSearchParams({
			response_type: 'code',
			client_id: jwt.clientId,
			redirect_uri: redirectUri,
			scope: jwt.scopes,
			state,
			code_challenge: challenge,
			code_challenge_method: 'S256'
		});

		window.location.href = `${jwt.authorizeUrl}?${params}`;
	}
</script>

<svelte:head>
	<title>Sign in - Konfigo</title>
</svelte:head>

<main class="login-background">
	<div class="w-full max-w-[420px] rounded-[8px] border border-[var(--border)] bg-[var(--bg-surface)] p-7 shadow-[var(--shadow-md)]">
		<div class="mb-7 flex items-center gap-3">
			<img src="/app-icon.png" alt="" class="h-12 w-12 rounded-[10px] border border-[var(--border)] bg-[var(--bg-elevated)] object-cover" />
			<div>
				<p class="section-label">Konfigo</p>
				<h1 class="mt-1 text-[22px] font-semibold text-[var(--text-primary)]">Sign in</h1>
			</div>
		</div>
		<p class="text-[14px] text-[var(--text-secondary)]">
			Use your organization account to enter the configuration control plane.
		</p>

		{#if data.authConfig.provider === 'jwt'}
			<button class="login-sso-button mt-6" type="button" onclick={startJwtLogin}>
				<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
					<path
						d="M8 1l5 2v5c0 4-2.5 5.5-5 7-2.5-1.5-5-3-5-7V3l5-2Z"
						stroke="currentColor"
						stroke-width="1.5"
						stroke-linejoin="round"
						stroke-linecap="round"
					/>
				</svg>
				<span>Login by SSO</span>
			</button>
		{:else}
			<a class="login-sso-button mt-6" href={loginHref}>
				<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
					<path
						d="M8 1l5 2v5c0 4-2.5 5.5-5 7-2.5-1.5-5-3-5-7V3l5-2Z"
						stroke="currentColor"
						stroke-width="1.5"
						stroke-linejoin="round"
						stroke-linecap="round"
					/>
				</svg>
				<span>Login by SSO</span>
			</a>
		{/if}
	</div>
</main>

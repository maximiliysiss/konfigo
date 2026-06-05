<script lang="ts">
	import { page } from '$app/stores';
	import { buildBackendUrl } from '$lib/api';

	const returnUrl = $derived.by(() => {
		const value = $page.url.searchParams.get('returnUrl');
		return value && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\')
			? value
			: '/services';
	});

	const loginHref = $derived(buildBackendUrl(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`));
</script>

<svelte:head>
	<title>Sign in — Realtime Config</title>
</svelte:head>

<main class="flex min-h-screen items-center justify-center bg-[var(--bg-app)] px-6">
	<div class="w-full max-w-[400px] rounded-[12px] border border-[var(--border)] bg-[var(--bg-surface)] p-8 shadow-sm">
		<p class="section-label">Realtime Config</p>
		<h1 class="mt-3 text-[20px] font-semibold text-[var(--text-primary)]">Sign in to continue</h1>
		<p class="mt-2 text-[14px] text-[var(--text-secondary)]">
			Use your JumpCloud account to sign in via SSO.
		</p>

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
	</div>
</main>
